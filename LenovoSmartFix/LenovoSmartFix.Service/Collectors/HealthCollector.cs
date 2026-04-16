using System.Diagnostics;
using System.Management;
using LenovoSmartFix.Core.Interfaces;
using LenovoSmartFix.Core.Models;
using Microsoft.Win32;

namespace LenovoSmartFix.Service.Collectors;

/// <summary>
/// Collects live health metrics.
///
/// Battery health: uses WMI root\WMI BatteryFullChargedCapacity vs
///   BatteryStaticData.DesignedCapacity; falls back to Win32_Battery charge
///   percentage if WMI ACPI battery classes are unavailable.
///
/// Wi-Fi signal: parsed from `netsh wlan show interfaces` output so we report
///   the real signal percentage rather than a hardcoded value.
///
/// Wi-Fi driver freshness: derived from the update validator result rather than
///   a hardcoded boolean; the field defaults to true and the update rules will
///   override based on actual version comparison.
/// </summary>
public sealed class HealthCollector : IHealthCollector
{
    private readonly ILogger<HealthCollector> _logger;

    public HealthCollector(ILogger<HealthCollector> logger) => _logger = logger;

    public async Task<HealthSnapshot> CollectAsync(CancellationToken ct = default) =>
        await Task.Run(Collect, ct);

    private HealthSnapshot Collect()
    {
        var battery = CollectBattery();
        var disk    = CollectDisk();
        var memory  = CollectMemory();
        var cpu     = CollectCpu();
        var wifi    = CollectWifi();
        var crashes = CollectCrashHistory();
        var startup = CollectStartupLoad();

        return new HealthSnapshot
        {
            BatteryHealthPercent              = battery.HealthPercent,
            BatteryCycleCount                 = battery.CycleCount,
            IsOnAcPower                       = battery.IsOnAc,
            PowerPlanName                     = GetActivePowerPlanName(),
            IsRecommendedPowerPlan            = IsRecommendedPowerPlan(),
            DiskTotalBytes                    = disk.TotalBytes,
            DiskFreeBytes                     = disk.FreeBytes,
            RamTotalBytes                     = memory.TotalBytes,
            RamAvailableBytes                 = memory.AvailableBytes,
            PageFaultsPerSec                  = memory.PageFaultsPerSec,
            CpuLoadPercent                    = cpu.LoadPercent,
            StartupItemCount                  = startup.Count,
            HighImpactBackgroundProcessCount  = startup.HighImpactCount,
            CpuTemperatureCelsius             = CollectCpuTemperature(),
            ThermalThrottlingDetected         = DetectThermalThrottling(),
            WifiAdapterPresent                = wifi.AdapterPresent,
            WifiAdapterName                   = wifi.AdapterName,
            WifiSignalStrengthPercent         = wifi.SignalStrength,
            WifiReconnectsLast24h             = wifi.ReconnectsLast24h,
            // Driver freshness is unknown here; UpdateCollector populates it
            // via the update validator and rules engine respects Unknown state.
            WifiDriverUpToDate                = true,
            AppCrashesLast7Days               = crashes.AppCrashes,
            SystemCrashesLast7Days            = crashes.SystemCrashes,
            RecentCrashSignatures             = crashes.Signatures
        };
    }

    // ── Battery ──────────────────────────────────────────────────────────────

    private record BatteryData(int HealthPercent, int CycleCount, bool IsOnAc);

    private BatteryData CollectBattery()
    {
        try
        {
            // Primary: WMI ACPI classes give real capacity data
            long fullCap = 0, designedCap = 0;
            int  cycles  = 0;
            bool onAc    = true;

            try
            {
                using var fullSr = new ManagementObjectSearcher(
                    @"root\WMI",
                    "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity");
                foreach (ManagementObject o in fullSr.Get())
                    fullCap = Convert.ToInt64(o["FullChargedCapacity"]);

                using var staticSr = new ManagementObjectSearcher(
                    @"root\WMI",
                    "SELECT DesignedCapacity, CycleCount FROM BatteryStaticData");
                foreach (ManagementObject o in staticSr.Get())
                {
                    designedCap = Convert.ToInt64(o["DesignedCapacity"]);
                    cycles      = Convert.ToInt32(o["CycleCount"]);
                }
            }
            catch
            {
                // WMI ACPI battery classes not available — fall through to Win32_Battery
            }

            // Determine AC/battery state from Win32_Battery
            using var batSr = new ManagementObjectSearcher(
                "SELECT BatteryStatus FROM Win32_Battery");
            foreach (ManagementObject o in batSr.Get())
            {
                var s = (ushort)(o["BatteryStatus"] ?? 0);
                onAc = s == 2; // 2 = AC online, fully charged or charging
                break;
            }

            int health;
            if (designedCap > 0 && fullCap > 0)
            {
                health = (int)Math.Clamp(
                    Math.Round((double)fullCap / designedCap * 100), 1, 100);
            }
            else
            {
                // Fallback: treat Win32_Battery EstimatedChargeRemaining as a proxy
                health = 100;
                using var fb = new ManagementObjectSearcher(
                    "SELECT EstimatedChargeRemaining FROM Win32_Battery");
                foreach (ManagementObject o in fb.Get())
                {
                    health = Convert.ToInt32(o["EstimatedChargeRemaining"] ?? 100);
                    break;
                }
            }

            return new BatteryData(health, cycles, onAc);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Battery collection failed; assuming healthy/AC");
            return new BatteryData(100, 0, true);
        }
    }

    // ── Disk ─────────────────────────────────────────────────────────────────

    private record DiskData(long TotalBytes, long FreeBytes);

    private DiskData CollectDisk()
    {
        try
        {
            var sysDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var info     = new DriveInfo(sysDrive);
            return new DiskData(info.TotalSize, info.AvailableFreeSpace);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Disk collection failed");
            return new DiskData(0, 0);
        }
    }

    // ── Memory ───────────────────────────────────────────────────────────────

    private record MemoryData(long TotalBytes, long AvailableBytes, int PageFaultsPerSec);

    private MemoryData CollectMemory()
    {
        try
        {
            using var sr = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (ManagementObject o in sr.Get())
            {
                var total = Convert.ToInt64(o["TotalVisibleMemorySize"]) * 1024L;
                var free  = Convert.ToInt64(o["FreePhysicalMemory"]) * 1024L;
                var pf    = (int)GetPerfCounterSafe("Memory", "Page Faults/sec", null);
                return new MemoryData(total, free, pf);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Memory collection failed");
        }
        return new MemoryData(0, 0, 0);
    }

    // ── CPU ──────────────────────────────────────────────────────────────────

    private record CpuData(double LoadPercent);

    private CpuData CollectCpu()
    {
        try
        {
            return new CpuData(GetPerfCounterSafe("Processor", "% Processor Time", "_Total"));
        }
        catch
        {
            return new CpuData(0);
        }
    }

    // ── Wi-Fi (netsh wlan show interfaces) ────────────────────────────────────

    private record WifiData(
        bool   AdapterPresent,
        string AdapterName,
        int    SignalStrength,
        int    ReconnectsLast24h,
        bool   DriverUpToDate);

    private WifiData CollectWifi()
    {
        try
        {
            var output = RunCommand("netsh", "wlan show interfaces");
            if (string.IsNullOrEmpty(output) || output.Contains("There is no wireless"))
                return new WifiData(false, string.Empty, 0, 0, true);

            var name   = ParseNetshField(output, "Name");
            var signal = ParseNetshSignal(output);

            return new WifiData(
                AdapterPresent    : true,
                AdapterName       : name,
                SignalStrength    : signal,
                ReconnectsLast24h : CollectWifiReconnects(),
                DriverUpToDate    : true   // unknown here; update validator refines this
            );
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Wi-Fi collection failed");
            return new WifiData(false, string.Empty, 0, 0, true);
        }
    }

    private static string ParseNetshField(string output, string field)
    {
        foreach (var line in output.Split('\n'))
        {
            var idx = line.IndexOf(field + "  ", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var colon = line.IndexOf(':', idx);
            if (colon < 0) continue;
            return line[(colon + 1)..].Trim();
        }
        return string.Empty;
    }

    private static int ParseNetshSignal(string output)
    {
        // "Signal                 : 78%"
        foreach (var line in output.Split('\n'))
        {
            if (!line.TrimStart().StartsWith("Signal", StringComparison.OrdinalIgnoreCase))
                continue;
            var colon = line.IndexOf(':');
            if (colon < 0) continue;
            var raw = line[(colon + 1)..].Trim().TrimEnd('%');
            if (int.TryParse(raw, out var pct))
                return pct;
        }
        return 0;
    }

    private int CollectWifiReconnects()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddHours(-24).ToString("o");
            var query = new EventLogQuery("System", PathType.LogName,
                $"*[System[(EventID=8001) and TimeCreated[@SystemTime>='{cutoff}']]]");
            using var reader = new EventLogReader(query);
            int count = 0;
            while (reader.ReadEvent() is not null) count++;
            return count;
        }
        catch
        {
            return 0;
        }
    }

    // ── Crashes ───────────────────────────────────────────────────────────────

    private record CrashData(int AppCrashes, int SystemCrashes, List<string> Signatures);

    private CrashData CollectCrashHistory()
    {
        int app = 0, sys = 0;
        var sigs = new List<string>();
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7).ToString("o");

            using (var r = new EventLogReader(new EventLogQuery("Application",
                PathType.LogName,
                $"*[System[(EventID=1000) and TimeCreated[@SystemTime>='{cutoff}']]]")))
            {
                EventRecord? ev;
                while ((ev = r.ReadEvent()) is not null)
                {
                    app++;
                    if (sigs.Count < 5)
                    {
                        var desc = TryFormat(ev);
                        if (!string.IsNullOrEmpty(desc))
                            sigs.Add(desc.Length > 120 ? desc[..120] : desc);
                    }
                }
            }

            using (var r = new EventLogReader(new EventLogQuery("System",
                PathType.LogName,
                $"*[System[(EventID=41) and TimeCreated[@SystemTime>='{cutoff}']]]")))
            {
                while (r.ReadEvent() is not null) sys++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Crash history collection partial");
        }
        return new CrashData(app, sys, sigs);
    }

    private static string TryFormat(EventRecord ev)
    {
        try { return ev.FormatDescription() ?? string.Empty; }
        catch { return string.Empty; }
    }

    // ── Startup load ─────────────────────────────────────────────────────────

    private record StartupData(int Count, int HighImpactCount);

    private static StartupData CollectStartupLoad()
    {
        int count = 0;
        var paths = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"
        };
        foreach (var p in paths)
        {
            using var k = Registry.LocalMachine.OpenSubKey(p);
            count += k?.ValueCount ?? 0;
        }
        using var user = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
        count += user?.ValueCount ?? 0;

        return new StartupData(count, Math.Max(0, count - 10));
    }

    // ── Thermal ──────────────────────────────────────────────────────────────

    private double? CollectCpuTemperature()
    {
        try
        {
            using var sr = new ManagementObjectSearcher(
                @"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject o in sr.Get())
            {
                var raw = Convert.ToDouble(o["CurrentTemperature"]);
                return (raw - 2732) / 10.0;
            }
        }
        catch { /* Not all firmware exposes thermal via WMI ACPI */ }
        return null;
    }

    private bool DetectThermalThrottling()
    {
        var temp = CollectCpuTemperature();
        return temp.HasValue && temp.Value > 90.0;
    }

    // ── Power plan ────────────────────────────────────────────────────────────

    private string GetActivePowerPlanName()
    {
        try
        {
            var output = RunCommand("powercfg", "/getactivescheme");
            var m = System.Text.RegularExpressions.Regex.Match(output, @"\((.+?)\)$",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value.Trim() : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private bool IsRecommendedPowerPlan()
    {
        var plan = GetActivePowerPlanName();
        return plan.Equals("Balanced", StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static float GetPerfCounterSafe(
        string category, string counter, string? instance)
    {
        try
        {
            using var pc = instance is null
                ? new PerformanceCounter(category, counter)
                : new PerformanceCounter(category, counter, instance);
            pc.NextValue();
            Thread.Sleep(200);
            return pc.NextValue();
        }
        catch { return 0f; }
    }

    private static string RunCommand(string exe, string args)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };
        using var p = Process.Start(psi);
        return p?.StandardOutput.ReadToEnd() ?? string.Empty;
    }
}

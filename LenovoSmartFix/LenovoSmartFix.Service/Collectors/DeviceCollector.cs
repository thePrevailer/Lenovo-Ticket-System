using System.Management;
using LenovoSmartFix.Core.Interfaces;
using LenovoSmartFix.Core.Models;
using Microsoft.Win32;

namespace LenovoSmartFix.Service.Collectors;

/// <summary>
/// Collects device identity from WMI (hardware + OS) and the registry
/// (Lenovo utilities, driver inventory).
///
/// Win32_Product is never used — it triggers MSI reconfiguration scans and
/// is extremely slow. All software enumeration uses the Uninstall registry keys.
/// </summary>
public sealed class DeviceCollector : IDeviceCollector
{
    private readonly ILogger<DeviceCollector> _logger;

    public DeviceCollector(ILogger<DeviceCollector> logger) => _logger = logger;

    public async Task<DeviceProfile> CollectAsync(CancellationToken ct = default) =>
        await Task.Run(Collect, ct);

    private DeviceProfile Collect()
    {
        try
        {
            var cs   = WmiFirst("Win32_ComputerSystem", "Manufacturer", "Model");
            var bios = WmiFirst("Win32_BIOS",
                "SMBIOSBIOSVersion", "ReleaseDate", "SerialNumber");
            var os   = WmiFirst("Win32_OperatingSystem",
                "Caption", "BuildNumber", "OSArchitecture");

            // EC firmware version — optional, suppress if WMI class is missing
            var ecVersion = string.Empty;
            try
            {
                var ec = WmiFirst("Win32_PortableBattery", "ManufacturerName");
                // EC version is not reliably in Win32_PortableBattery on all models;
                // real collection would use Lenovo-specific WMI providers if available.
                ecVersion = string.Empty;
            }
            catch { /* not all models expose EC via WMI */ }

            return new DeviceProfile
            {
                DeviceId                 = Environment.MachineName,
                Manufacturer             = Get(cs,   "Manufacturer"),
                Model                    = Get(cs,   "Model"),
                MachineType              = ExtractMachineType(Get(cs, "Model")),
                SerialNumber             = Get(bios,  "SerialNumber"),
                BiosVersion              = Get(bios,  "SMBIOSBIOSVersion"),
                BiosDate                 = Get(bios,  "ReleaseDate"),
                EcFirmwareVersion        = ecVersion,
                OsVersion                = Get(os,   "Caption"),
                OsBuild                  = Get(os,   "BuildNumber"),
                OsEdition                = Get(os,   "OSArchitecture"),
                InstalledLenovoUtilities = CollectLenovoUtilitiesFromRegistry(),
                DriverInventory          = CollectDriverInventory()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Device collection partial — returning minimal profile");
            return new DeviceProfile
            {
                DeviceId  = Environment.MachineName,
                Model     = "Unknown",
                OsVersion = Environment.OSVersion.VersionString,
                OsBuild   = Environment.OSVersion.Version.Build.ToString()
            };
        }
    }

    // ── Lenovo utilities via registry (never Win32_Product) ─────────────────

    private Dictionary<string, string> CollectLenovoUtilitiesFromRegistry()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var paths = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var path in paths)
        {
            using var hive = Registry.LocalMachine.OpenSubKey(path);
            if (hive is null) continue;

            foreach (var subName in hive.GetSubKeyNames())
            {
                using var sub = hive.OpenSubKey(subName);
                if (sub is null) continue;

                var publisher = sub.GetValue("Publisher") as string ?? string.Empty;
                if (!publisher.Contains("Lenovo", StringComparison.OrdinalIgnoreCase))
                    continue;

                var name    = sub.GetValue("DisplayName")    as string ?? string.Empty;
                var version = sub.GetValue("DisplayVersion") as string ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(name))
                    result.TryAdd(name, version);
            }
        }

        return result;
    }

    // ── Driver inventory ─────────────────────────────────────────────────────

    private Dictionary<string, string> CollectDriverInventory()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceClass, DriverVersion FROM Win32_PnPSignedDriver " +
                "WHERE DeviceClass IS NOT NULL AND DeviceClass <> ''");

            foreach (ManagementObject obj in searcher.Get())
            {
                var cls = obj["DeviceClass"]?.ToString() ?? string.Empty;
                var ver = obj["DriverVersion"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(cls))
                    result.TryAdd(cls, ver);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Driver inventory WMI query failed");
        }
        return result;
    }

    // ── WMI helpers ──────────────────────────────────────────────────────────

    private static Dictionary<string, object?> WmiFirst(
        string wmiClass, params string[] props)
    {
        var result = new Dictionary<string, object?>();
        using var searcher = new ManagementObjectSearcher(
            $"SELECT {string.Join(",", props)} FROM {wmiClass}");
        foreach (ManagementObject obj in searcher.Get())
        {
            foreach (var p in props)
                result[p] = obj[p];
            break;
        }
        return result;
    }

    private static string Get(Dictionary<string, object?> d, string key) =>
        d.TryGetValue(key, out var v) && v is not null ? v.ToString()! : string.Empty;

    private static string ExtractMachineType(string model)
    {
        // "ThinkPad X1 Carbon Gen 11" → "ThinkPad X1"
        var parts = model.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? string.Join(" ", parts.Take(2))
            : model;
    }
}

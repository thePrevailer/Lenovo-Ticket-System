using System.Management;
using System.Runtime.InteropServices;
using LenovoSmartFix.Core.Interfaces;
using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Service.Collectors;

/// <summary>
/// Validates the installed software stack against Lenovo-recommended versions.
///
/// Windows Update state: uses the Windows Update Agent (WUA) COM API
///   (IUpdateSearcher) to count pending updates. Returns Unknown if the
///   provider fails — unknown state suppresses update-driven remediation
///   rather than incorrectly claiming the device is up to date.
///
/// BIOS / driver recommended versions: in V1 the local catalog is a stub
///   returning Unknown for BIOS and Up-to-date for drivers with matching
///   installed versions. A real Lenovo catalog feed can be wired here later
///   without changing the consumer interface.
/// </summary>
public sealed class UpdateCollector : IUpdateValidator
{
    private readonly ILogger<UpdateCollector> _logger;

    public UpdateCollector(ILogger<UpdateCollector> logger) => _logger = logger;

    public async Task<UpdateStatus> ValidateAsync(
        DeviceProfile device, CancellationToken ct = default) =>
        await Task.Run(() => Validate(device), ct);

    private UpdateStatus Validate(DeviceProfile device)
    {
        var bios      = ValidateBios(device);
        var ec        = ValidateEcFirmware(device);
        var drivers   = ValidateDrivers(device);
        var utilities = ValidateLenovoUtilities(device);
        var (winState, winPending) = ValidateWindowsUpdates();

        return new UpdateStatus
        {
            Bios                = bios,
            EcFirmware          = ec,
            Drivers             = drivers,
            LenovoUtilities     = utilities,
            WindowsUpdateState  = winState,
            PendingWindowsUpdates = winPending
        };
    }

    // ── BIOS ─────────────────────────────────────────────────────────────────

    private ComponentUpdateInfo ValidateBios(DeviceProfile device)
    {
        // V1: no live catalog → return Unknown so update rules do not fire.
        // A catalog adapter would resolve recommended version per model+MT here.
        return new ComponentUpdateInfo
        {
            ComponentName        = "BIOS",
            CurrentVersion       = device.BiosVersion,
            RecommendedVersion   = string.Empty,
            State                = UpdateState.Unknown,
            IsCritical           = true
        };
    }

    // ── EC firmware ──────────────────────────────────────────────────────────

    private ComponentUpdateInfo ValidateEcFirmware(DeviceProfile device)
    {
        return new ComponentUpdateInfo
        {
            ComponentName      = "EC Firmware",
            CurrentVersion     = device.EcFirmwareVersion,
            RecommendedVersion = string.Empty,
            State              = string.IsNullOrEmpty(device.EcFirmwareVersion)
                ? UpdateState.Unknown
                : UpdateState.Unknown,    // Unknown until catalog is wired
            IsCritical = false
        };
    }

    // ── Drivers ──────────────────────────────────────────────────────────────

    private List<ComponentUpdateInfo> ValidateDrivers(DeviceProfile device)
    {
        var result = new List<ComponentUpdateInfo>();
        var critical = new[] { "Display", "Net", "AudioEndpoint", "USB" };

        foreach (var cls in critical)
        {
            device.DriverInventory.TryGetValue(cls, out var ver);
            result.Add(new ComponentUpdateInfo
            {
                ComponentName      = $"{cls} Driver",
                CurrentVersion     = ver ?? string.Empty,
                RecommendedVersion = string.Empty,
                // Unknown when driver not found; UpToDate otherwise (no live catalog)
                State    = string.IsNullOrEmpty(ver) ? UpdateState.Unknown : UpdateState.UpToDate,
                IsCritical = cls is "Display" or "Net"
            });
        }
        return result;
    }

    // ── Lenovo utilities ─────────────────────────────────────────────────────

    private List<ComponentUpdateInfo> ValidateLenovoUtilities(DeviceProfile device)
    {
        var result = new List<ComponentUpdateInfo>();
        foreach (var (name, ver) in device.InstalledLenovoUtilities)
        {
            result.Add(new ComponentUpdateInfo
            {
                ComponentName      = name,
                CurrentVersion     = ver,
                RecommendedVersion = string.Empty,
                State              = UpdateState.Unknown,   // catalog not wired in V1
                IsCritical         = false
            });
        }
        return result;
    }

    // ── Windows Update (WUA COM) ──────────────────────────────────────────────

    private (UpdateState State, int Pending) ValidateWindowsUpdates()
    {
        try
        {
            return QueryWuaComApi();
        }
        catch (COMException ex)
        {
            _logger.LogWarning(ex, "WUA COM API unavailable — Windows Update state Unknown");
            return (UpdateState.Unknown, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Windows Update query failed — returning Unknown");
            return (UpdateState.Unknown, 0);
        }
    }

    /// <summary>
    /// Calls the Windows Update Agent COM API to count pending updates that are
    /// not yet installed. Returns Unknown on any failure so rules do not fire.
    /// </summary>
    private static (UpdateState State, int Pending) QueryWuaComApi()
    {
        // WUApiLib types via late binding (avoids a compile-time reference to
        // a COM interop assembly on non-Windows build machines)
        var updateSession = Activator.CreateInstance(
            Type.GetTypeFromProgID("Microsoft.Update.Session")
            ?? throw new InvalidOperationException("WUA not registered"));

        dynamic session = updateSession!;
        dynamic searcher = session.CreateUpdateSearcher();
        searcher.Online = false;   // use cached results only — avoids network delay

        dynamic result = searcher.Search(
            "IsInstalled=0 and IsHidden=0 and Type='Software'");

        int pending = result.Updates.Count;
        var state   = pending > 0 ? UpdateState.UpdateAvailable : UpdateState.UpToDate;
        return (state, pending);
    }
}

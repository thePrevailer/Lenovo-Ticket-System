namespace LenovoSmartFix.Core.Models;

public enum UpdateState
{
    UpToDate,
    UpdateAvailable,
    Critical,
    Unknown
}

public sealed class ComponentUpdateInfo
{
    public string ComponentName { get; init; } = string.Empty;
    public string CurrentVersion { get; init; } = string.Empty;
    public string RecommendedVersion { get; init; } = string.Empty;
    public UpdateState State { get; init; }
    public bool IsCritical { get; init; }
    public string? UpdateUrl { get; init; }
}

/// <summary>
/// Compares installed stack against Lenovo-recommended versions.
/// </summary>
public sealed class UpdateStatus
{
    public ComponentUpdateInfo Bios { get; init; } = new();
    public ComponentUpdateInfo EcFirmware { get; init; } = new();
    public List<ComponentUpdateInfo> Drivers { get; init; } = new();
    public List<ComponentUpdateInfo> LenovoUtilities { get; init; } = new();
    public UpdateState WindowsUpdateState { get; init; }
    public int PendingWindowsUpdates { get; init; }
    public bool HasCriticalUpdates =>
        Bios.State == UpdateState.Critical
        || EcFirmware.State == UpdateState.Critical
        || Drivers.Any(d => d.State == UpdateState.Critical)
        || LenovoUtilities.Any(u => u.State == UpdateState.Critical);

    public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;
}

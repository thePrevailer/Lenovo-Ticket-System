namespace LenovoSmartFix.Core.Models;

/// <summary>
/// Point-in-time health metrics across battery, storage, memory, CPU, network, and stability.
/// </summary>
public sealed class HealthSnapshot
{
    public string SnapshotId { get; init; } = Guid.NewGuid().ToString();

    // Battery
    public int BatteryHealthPercent { get; init; }         // 0-100
    public int BatteryCycleCount { get; init; }
    public bool IsOnAcPower { get; init; }
    public string PowerPlanName { get; init; } = string.Empty;
    public bool IsRecommendedPowerPlan { get; init; }

    // Storage
    public long DiskTotalBytes { get; init; }
    public long DiskFreeBytes { get; init; }
    public double DiskUsedPercent => DiskTotalBytes > 0
        ? Math.Round((1.0 - (double)DiskFreeBytes / DiskTotalBytes) * 100, 1)
        : 0;

    // Memory
    public long RamTotalBytes { get; init; }
    public long RamAvailableBytes { get; init; }
    public double RamUsedPercent => RamTotalBytes > 0
        ? Math.Round((1.0 - (double)RamAvailableBytes / RamTotalBytes) * 100, 1)
        : 0;
    public int PageFaultsPerSec { get; init; }

    // CPU
    public double CpuLoadPercent { get; init; }            // 0-100
    public int StartupItemCount { get; init; }
    public int HighImpactBackgroundProcessCount { get; init; }

    // Thermal
    public double? CpuTemperatureCelsius { get; init; }
    public bool ThermalThrottlingDetected { get; init; }

    // Wi-Fi
    public bool WifiAdapterPresent { get; init; }
    public string WifiAdapterName { get; init; } = string.Empty;
    public int WifiSignalStrengthPercent { get; init; }
    public int WifiReconnectsLast24h { get; init; }
    public bool WifiDriverUpToDate { get; init; }

    // Crash / stability
    public int AppCrashesLast7Days { get; init; }
    public int SystemCrashesLast7Days { get; init; }
    public List<string> RecentCrashSignatures { get; init; } = new();

    public DateTimeOffset CollectedAt { get; init; } = DateTimeOffset.UtcNow;
}

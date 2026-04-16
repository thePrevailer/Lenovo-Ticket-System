namespace LenovoSmartFix.Service;

public sealed class SmartFixOptions
{
    public string DatabasePath { get; set; } = "%LOCALAPPDATA%\\LenovoSmartFix\\smartfix.db";
    public string LogDirectory { get; set; } = "%LOCALAPPDATA%\\LenovoSmartFix\\Logs";
    public int ScanHistoryRetentionDays { get; set; } = 30;
    public string IpcPipeName { get; set; } = "LenovoSmartFixPipe";
    public ThresholdOptions Thresholds { get; set; } = new();
}

public sealed class ThresholdOptions
{
    public double DiskUsedWarningPercent { get; set; } = 85.0;
    public double DiskUsedCriticalPercent { get; set; } = 95.0;
    public double RamUsedWarningPercent { get; set; } = 85.0;
    public double RamUsedCriticalPercent { get; set; } = 95.0;
    public double CpuLoadWarningPercent { get; set; } = 80.0;
    public int BatteryHealthWarningPercent { get; set; } = 60;
    public int BatteryHealthCriticalPercent { get; set; } = 40;
    public int WifiReconnectWarningCount { get; set; } = 5;
    public int AppCrashesWarningCount { get; set; } = 3;
    public int StartupItemsWarningCount { get; set; } = 15;
}

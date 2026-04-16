using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LenovoSmartFix.Core.Models;
using LenovoSmartFix.UI.Pages;
using LenovoSmartFix.UI.Services;

namespace LenovoSmartFix.UI.ViewModels;

public sealed partial class FindingsViewModel : ViewModelBase
{
    private readonly INavigationService _nav;
    private ScanResult? _scanResult;

    // Device
    [ObservableProperty] private string _deviceModel = string.Empty;
    [ObservableProperty] private string _osInfo = string.Empty;
    [ObservableProperty] private string _biosVersion = string.Empty;

    // Health highlights
    [ObservableProperty] private string _batteryStatus = string.Empty;
    [ObservableProperty] private string _diskStatus = string.Empty;
    [ObservableProperty] private string _ramStatus = string.Empty;
    [ObservableProperty] private string _cpuStatus = string.Empty;
    [ObservableProperty] private string _wifiStatus = string.Empty;
    [ObservableProperty] private string _crashStatus = string.Empty;

    // Diagnosis
    [ObservableProperty] private string _diagnosisPath = string.Empty;
    [ObservableProperty] private string _diagnosisReason = string.Empty;
    [ObservableProperty] private bool _needsEscalation;
    [ObservableProperty] private bool _hasUpdates;
    [ObservableProperty] private string _updateSummary = string.Empty;

    public FindingsViewModel(INavigationService nav) => _nav = nav;

    public void LoadScanResult(ScanResult result)
    {
        _scanResult = result;

        var d = result.DeviceProfile;
        var h = result.HealthSnapshot;
        var u = result.UpdateStatus;
        var dec = result.Decision;

        if (d is not null)
        {
            DeviceModel = d.Model;
            OsInfo = $"{d.OsVersion} (Build {d.OsBuild})";
            BiosVersion = d.BiosVersion;
        }

        if (h is not null)
        {
            BatteryStatus = $"{h.BatteryHealthPercent}% health  ·  " +
                (h.IsOnAcPower ? "Plugged in" : "On battery");
            DiskStatus = $"{h.DiskUsedPercent:F0}% used  ·  " +
                $"{h.DiskFreeBytes / 1_073_741_824.0:F1} GB free";
            RamStatus = $"{h.RamUsedPercent:F0}% used";
            CpuStatus = $"{h.CpuLoadPercent:F0}% load  ·  " +
                $"{h.StartupItemCount} startup items";
            WifiStatus = h.WifiAdapterPresent
                ? $"{h.WifiReconnectsLast24h} drops (24h)  ·  {h.WifiAdapterName}"
                : "No wireless adapter";
            CrashStatus = $"{h.AppCrashesLast7Days} app crashes  ·  " +
                $"{h.SystemCrashesLast7Days} system crashes (7 days)";
        }

        if (u is not null)
        {
            HasUpdates = u.HasCriticalUpdates || u.PendingWindowsUpdates > 0;
            var outdated = u.Drivers.Count(x => x.State == UpdateState.UpdateAvailable)
                + u.LenovoUtilities.Count(x => x.State == UpdateState.UpdateAvailable);
            UpdateSummary = HasUpdates
                ? $"BIOS: {u.Bios.State}  ·  {outdated} driver/utility update(s) available"
                : "Software stack is up to date";
        }

        if (dec is not null)
        {
            DiagnosisPath = dec.Path.ToString();
            DiagnosisReason = dec.UserFacingReason;
            NeedsEscalation = dec.Path == Core.Models.DiagnosisPath.Escalate;
        }
    }

    [RelayCommand]
    private void ViewResolutionOptions()
    {
        if (_scanResult is not null)
            _nav.NavigateTo(typeof(ResolutionCenterPage), _scanResult);
    }

    [RelayCommand]
    private void ViewEscalationPacket()
    {
        if (_scanResult is not null)
            _nav.NavigateTo(typeof(EscalationPacketPage), _scanResult);
    }
}

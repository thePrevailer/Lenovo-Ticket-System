using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LenovoSmartFix.Core.Models;
using LenovoSmartFix.UI.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace LenovoSmartFix.UI.ViewModels;

public sealed partial class EscalationPacketViewModel : ViewModelBase
{
    private readonly ISmartFixServiceProxy _service;
    private ScanResult? _scanResult;
    private EscalationPacket? _packet;

    [ObservableProperty] private string _symptom = string.Empty;
    [ObservableProperty] private string _outcome = string.Empty;
    [ObservableProperty] private string _unresolvedReason = string.Empty;
    [ObservableProperty] private string _exportedJsonPath = string.Empty;
    [ObservableProperty] private string _exportedPdfPath = string.Empty;
    [ObservableProperty] private bool _exportCompleted;
    [ObservableProperty] private bool _includePersonalData;

    public EscalationPacketViewModel(ISmartFixServiceProxy service) => _service = service;

    public async Task LoadAsync(ScanResult result, CancellationToken ct = default)
    {
        _scanResult = result;
        IsBusy = true;
        StatusMessage = "Building support packet...";

        try
        {
            _packet = await _service.BuildEscalationPacketAsync(
                result.ScanId, redact: !IncludePersonalData, ct);
            Symptom = _packet.PrimarySymptom;
            Outcome = _packet.Outcome;
            UnresolvedReason = _packet.UnresolvedReason;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not build packet: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            if (string.IsNullOrEmpty(StatusMessage) || StatusMessage.StartsWith("Could"))
            {
                // keep error message visible
            }
            else StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (_scanResult is null) return;

        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null) return;

        IsBusy = true;
        StatusMessage = "Exporting...";
        try
        {
            var (json, pdf) = await _service.ExportEscalationPacketAsync(
                _scanResult.ScanId, folder.Path,
                includePdf: true, redact: !IncludePersonalData);
            ExportedJsonPath = json;
            ExportedPdfPath = pdf ?? string.Empty;
            ExportCompleted = true;
            StatusMessage = "Export complete.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        if (!string.IsNullOrEmpty(ExportedJsonPath))
        {
            var dir = System.IO.Path.GetDirectoryName(ExportedJsonPath);
            if (dir is not null)
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo("explorer.exe", dir)
                    { UseShellExecute = true });
        }
    }
}

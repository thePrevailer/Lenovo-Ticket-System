using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LenovoSmartFix.Core.Models;
using LenovoSmartFix.UI.Pages;
using LenovoSmartFix.UI.Services;

namespace LenovoSmartFix.UI.ViewModels;

public sealed partial class ScanProgressViewModel : ViewModelBase
{
    private readonly ISmartFixServiceProxy _service;
    private readonly INavigationService _nav;

    [ObservableProperty] private int    _progressPercent;
    [ObservableProperty] private string _currentStep  = "Preparing scan...";
    [ObservableProperty] private bool   _scanFailed;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public ScanProgressViewModel(ISmartFixServiceProxy service, INavigationService nav)
    {
        _service = service;
        _nav     = nav;
    }

    /// <summary>
    /// Starts the scan (non-blocking on the service side) then polls
    /// GetScanStatusAsync every 600 ms until the scan reaches a terminal state.
    /// Cancelling <paramref name="ct"/> stops polling immediately.
    /// </summary>
    public async Task StartAndPollAsync(string symptom, CancellationToken ct = default)
    {
        IsBusy     = true;
        ScanFailed = false;
        ErrorMessage = string.Empty;
        ProgressPercent = 0;
        CurrentStep = "Starting scan…";

        try
        {
            var scanId = await _service.InitiateScanAsync(symptom, ct);

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(600, ct);

                var status = await _service.GetScanStatusAsync(scanId, ct);

                ProgressPercent = status.ProgressPercent;
                CurrentStep     = status.ProgressStep;

                if (status.Status == ScanStatus.Completed && status.Result is not null)
                {
                    _nav.NavigateTo(typeof(FindingsPage), status.Result);
                    return;
                }

                if (status.Status is ScanStatus.Failed or ScanStatus.Cancelled)
                {
                    ScanFailed   = true;
                    ErrorMessage = status.ErrorMessage ?? "The scan could not complete.";
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Navigation away — no-op
        }
        catch (Exception ex)
        {
            ScanFailed   = true;
            ErrorMessage = $"Scan could not complete: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

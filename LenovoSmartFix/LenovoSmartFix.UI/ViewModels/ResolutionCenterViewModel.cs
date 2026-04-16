using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LenovoSmartFix.Core.Models;
using LenovoSmartFix.UI.Pages;
using LenovoSmartFix.UI.Services;

namespace LenovoSmartFix.UI.ViewModels;

public sealed partial class ResolutionCenterViewModel : ViewModelBase
{
    private readonly ISmartFixServiceProxy _service;
    private readonly INavigationService _nav;
    private ScanResult? _scanResult;

    public ObservableCollection<ActionItem> Actions { get; } = new();

    [ObservableProperty] private string _diagnosisPath   = string.Empty;
    [ObservableProperty] private string _diagnosisReason = string.Empty;

    public ResolutionCenterViewModel(
        ISmartFixServiceProxy service, INavigationService nav)
    {
        _service = service;
        _nav     = nav;
    }

    public void LoadScanResult(ScanResult result)
    {
        _scanResult = result;

        if (result.Decision is not null)
        {
            DiagnosisPath   = result.Decision.Path.ToString();
            DiagnosisReason = result.Decision.UserFacingReason;
        }

        Actions.Clear();
        foreach (var action in result.Actions)
            Actions.Add(new ActionItem(action));
    }

    [RelayCommand]
    private async Task ExecuteActionAsync(ActionItem item)
    {
        if (item is null || item.IsExecuting || _scanResult is null) return;
        item.IsExecuting = true;
        IsBusy           = true;
        StatusMessage    = $"Running: {item.ActionName}...";

        try
        {
            // Clicking the button constitutes explicit consent for Consent-level actions.
            var updated = await _service.ExecuteRemediationAsync(
                _scanResult.ScanId, item.ActionInstanceId, userConsented: true);

            item.Result       = updated.Result.ToString();
            item.ResultDetail = updated.ResultDetail ?? string.Empty;
            item.IsCompleted  = true;
        }
        catch (Exception ex)
        {
            item.Result       = "Failed";
            item.ResultDetail = ex.Message;
        }
        finally
        {
            item.IsExecuting = false;
            IsBusy           = false;
            StatusMessage    = string.Empty;
        }
    }

    [RelayCommand]
    private void GoToEscalation()
    {
        if (_scanResult is not null)
            _nav.NavigateTo(typeof(EscalationPacketPage), _scanResult);
    }
}

public sealed partial class ActionItem : ObservableObject
{
    public string ActionInstanceId { get; }   // per-scan UUID — used for IPC calls
    public string ActionId         { get; }   // stable library code — display only
    public string ActionName       { get; }
    public string Description      { get; }
    public bool   RequiresConsent  { get; }
    public string SafetyBadge      { get; }

    [ObservableProperty] private bool   _isExecuting;
    [ObservableProperty] private bool   _isCompleted;
    [ObservableProperty] private string _result       = string.Empty;
    [ObservableProperty] private string _resultDetail = string.Empty;

    public ActionItem(RemediationAction action)
    {
        ActionInstanceId = action.ActionInstanceId;
        ActionId         = action.ActionId;
        ActionName       = action.ActionName;
        Description      = action.Description;
        RequiresConsent  = action.ConsentRequired;
        SafetyBadge      = action.SafetyLevel switch
        {
            RemediationSafetyLevel.Safe    => "Auto",
            RemediationSafetyLevel.Consent => "Requires confirmation",
            _                              => "Guided"
        };
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LenovoSmartFix.UI.Pages;
using LenovoSmartFix.UI.Services;

namespace LenovoSmartFix.UI.ViewModels;

public sealed partial class OverviewViewModel : ViewModelBase
{
    private readonly INavigationService _nav;

    public static readonly IReadOnlyList<string> SymptomOptions = new[]
    {
        "Slow performance",
        "Battery draining fast",
        "Overheating",
        "Unstable Wi-Fi",
        "App or system crashes",
        "Storage almost full",
        "Device feels sluggish after update",
        "Other / general instability"
    };

    [ObservableProperty]
    private string _selectedSymptom = SymptomOptions[0];

    public OverviewViewModel(INavigationService nav) => _nav = nav;

    [RelayCommand]
    private void StartScan()
    {
        if (string.IsNullOrEmpty(SelectedSymptom)) return;
        _nav.NavigateTo(typeof(ScanProgressPage), SelectedSymptom);
    }
}

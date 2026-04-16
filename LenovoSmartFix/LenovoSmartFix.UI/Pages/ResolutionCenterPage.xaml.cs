using LenovoSmartFix.Core.Models;
using LenovoSmartFix.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace LenovoSmartFix.UI.Pages;

public sealed partial class ResolutionCenterPage : Page
{
    public ResolutionCenterViewModel ViewModel { get; }

    public ResolutionCenterPage()
    {
        ViewModel = App.Services.GetService<ResolutionCenterViewModel>()!;
        InitializeComponent();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ScanResult result)
            ViewModel.LoadScanResult(result);
    }
}

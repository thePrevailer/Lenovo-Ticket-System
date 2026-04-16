using LenovoSmartFix.Core.Models;
using LenovoSmartFix.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace LenovoSmartFix.UI.Pages;

public sealed partial class EscalationPacketPage : Page
{
    public EscalationPacketViewModel ViewModel { get; }

    public EscalationPacketPage()
    {
        ViewModel = App.Services.GetService<EscalationPacketViewModel>()!;
        InitializeComponent();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ScanResult result)
            _ = ViewModel.LoadAsync(result);
    }
}

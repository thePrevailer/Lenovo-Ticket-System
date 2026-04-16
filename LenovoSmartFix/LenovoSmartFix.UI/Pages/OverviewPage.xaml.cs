using LenovoSmartFix.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace LenovoSmartFix.UI.Pages;

public sealed partial class OverviewPage : Page
{
    public OverviewViewModel ViewModel { get; }

    public OverviewPage()
    {
        ViewModel = App.Services.GetService<OverviewViewModel>()!;
        InitializeComponent();
        DataContext = ViewModel;
    }
}

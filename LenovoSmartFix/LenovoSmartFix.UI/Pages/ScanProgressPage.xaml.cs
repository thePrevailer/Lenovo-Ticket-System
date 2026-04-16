using LenovoSmartFix.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace LenovoSmartFix.UI.Pages;

public sealed partial class ScanProgressPage : Page
{
    public ScanProgressViewModel ViewModel { get; }

    private string _symptom = string.Empty;
    private CancellationTokenSource? _cts;

    public ScanProgressPage()
    {
        ViewModel = App.Services.GetService<ScanProgressViewModel>()!;
        InitializeComponent();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _symptom = e.Parameter as string ?? string.Empty;
        BeginScan();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        // Cancel polling when navigating away (including forward to FindingsPage)
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void RetryButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        BeginScan();
    }

    private void BeginScan()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _ = ViewModel.StartAndPollAsync(_symptom, _cts.Token);
    }
}

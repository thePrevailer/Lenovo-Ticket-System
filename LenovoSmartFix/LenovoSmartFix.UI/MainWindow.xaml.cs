using LenovoSmartFix.UI.Pages;
using LenovoSmartFix.UI.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace LenovoSmartFix.UI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetWindowSize(960, 660);

        // Wire navigation service to this window's frame
        var nav = App.Services.GetService<INavigationService>() as NavigationService;
        nav?.Initialize(MainFrame);

        // Start on the Overview page
        MainFrame.Navigate(typeof(OverviewPage));
    }

    private void SetWindowSize(int width, int height)
    {
        var appWindow = AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(
                WinRT.Interop.WindowNative.GetWindowHandle(this)));
        appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
    }
}

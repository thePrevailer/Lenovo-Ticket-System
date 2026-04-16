using LenovoSmartFix.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace LenovoSmartFix.UI;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // IPC proxy (talks to SmartFixService via named pipe)
        services.AddSingleton<ISmartFixServiceProxy, SmartFixServiceProxy>();

        // Navigation
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels
        services.AddTransient<ViewModels.OverviewViewModel>();
        services.AddTransient<ViewModels.ScanProgressViewModel>();
        services.AddTransient<ViewModels.FindingsViewModel>();
        services.AddTransient<ViewModels.ResolutionCenterViewModel>();
        services.AddTransient<ViewModels.EscalationPacketViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>The application's single main window. Available after OnLaunched.</summary>
    public static Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}

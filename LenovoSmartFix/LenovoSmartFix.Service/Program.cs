using LenovoSmartFix.Core.Interfaces;
using LenovoSmartFix.Service.Collectors;
using LenovoSmartFix.Service.Engine;
using LenovoSmartFix.Service.Escalation;
using LenovoSmartFix.Service.IPC;
using LenovoSmartFix.Service.Persistence;
using LenovoSmartFix.Service.Remediation;
using LenovoSmartFix.Service.Rules;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace LenovoSmartFix.Service;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = "LenovoSmartFixService";
            })
            .UseSerilog((ctx, cfg) =>
            {
                var logDir = Environment.ExpandEnvironmentVariables(
                    ctx.Configuration["SmartFix:LogDirectory"] ?? "%TEMP%\\LenovoSmartFix\\Logs");
                Directory.CreateDirectory(logDir);
                cfg.ReadFrom.Configuration(ctx.Configuration)
                   .WriteTo.Console()
                   .WriteTo.File(
                       Path.Combine(logDir, "smartfix-.log"),
                       rollingInterval: RollingInterval.Day,
                       retainedFileCountLimit: 7);
            })
            .ConfigureServices((ctx, services) =>
            {
                var cfg = ctx.Configuration;

                // Settings
                services.Configure<SmartFixOptions>(cfg.GetSection("SmartFix"));

                // Database
                var dbPath = Environment.ExpandEnvironmentVariables(
                    cfg["SmartFix:DatabasePath"] ?? "%LOCALAPPDATA%\\LenovoSmartFix\\smartfix.db");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                services.AddDbContext<SmartFixDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

                // Collectors
                services.AddTransient<IDeviceCollector, DeviceCollector>();
                services.AddTransient<IHealthCollector, HealthCollector>();
                services.AddTransient<IUpdateValidator, UpdateCollector>();

                // Rules + engine
                services.AddSingleton<IRuleEngine, RulesEngine>();

                // Remediation
                services.AddTransient<RemediationExecutor>();

                // Escalation
                services.AddTransient<EscalationPacketBuilder>();

                // Repository
                services.AddScoped<SmartFixRepository>();

                // Core service
                services.AddTransient<ISmartFixService, SmartFixCoreService>();

                // IPC server (named pipe)
                services.AddSingleton<NamedPipeServer>();
                services.AddHostedService<SmartFixWorker>();
            })
            .Build();

        // Apply EF migrations on startup
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartFixDbContext>();
            await db.Database.MigrateAsync();
        }

        await host.RunAsync();
    }
}

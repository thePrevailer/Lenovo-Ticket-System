using System.Diagnostics;
using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Service.Remediation;

/// <summary>
/// Executes approved remediation actions from the V1 library.
///
/// Safety rules:
/// - Safe-level actions run without consent.
/// - Consent-level actions are blocked if <paramref name="userConsented"/> is false.
/// - Vantage launch is always Consent-level and is never called automatically.
/// - All command-based actions capture exit code and stderr; non-zero exits set
///   the result to Failed with detail from stderr so support packets are accurate.
/// </summary>
public sealed class RemediationExecutor
{
    private readonly ILogger<RemediationExecutor> _logger;

    public RemediationExecutor(ILogger<RemediationExecutor> logger) => _logger = logger;

    public async Task<RemediationAction> ExecuteAsync(
        RemediationAction action, bool userConsented, CancellationToken ct = default)
    {
        if (action.ConsentRequired && !userConsented)
        {
            _logger.LogWarning(
                "Blocked {ActionId} — consent required but not given", action.ActionId);
            action.Result       = RemediationResult.Skipped;
            action.ResultDetail = "User consent required but not provided.";
            return action;
        }

        action.UserConsented = userConsented;
        action.ExecutedAt    = DateTimeOffset.UtcNow;

        _logger.LogInformation("Executing {ActionId} ({ActionName})",
            action.ActionId, action.ActionName);

        try
        {
            await DispatchAsync(action, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remediation {ActionId} threw unexpectedly", action.ActionId);
            action.Result       = RemediationResult.Failed;
            action.ResultDetail = $"Unexpected error: {ex.Message}";
        }

        return action;
    }

    // ── Dispatch ──────────────────────────────────────────────────────────────

    private Task DispatchAsync(RemediationAction action, CancellationToken ct) =>
        action.ActionId switch
        {
            "REM-TEMP-CLEANUP"   => CleanTempFilesAsync(action, ct),
            "REM-POWER-PLAN"     => SetBalancedPowerPlanAsync(action, ct),
            "REM-DNS-FLUSH"      => FlushDnsAsync(action, ct),
            "REM-NET-SVC"        => RestartNetworkServicesAsync(action, ct),
            "REM-STARTUP-OPT"    => OpenStartupManagerAsync(action),
            "REM-UPDATE-VANTAGE" => LaunchVantageUpdateAsync(action),
            _                    => UnknownActionAsync(action)
        };

    // ── Action implementations ─────────────────────────────────────────────────

    private async Task CleanTempFilesAsync(RemediationAction action, CancellationToken ct)
    {
        long freed = 0;
        var tempPath = Path.GetTempPath();

        await Task.Run(() =>
        {
            foreach (var file in Directory.EnumerateFiles(
                tempPath, "*", SearchOption.TopDirectoryOnly))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(file);
                    freed += fi.Length;
                    fi.Delete();
                }
                catch { /* skip locked/system files */ }
            }
        }, ct);

        action.Result       = RemediationResult.Success;
        action.ResultDetail = $"Freed {freed / 1_048_576.0:F1} MB of temporary files.";
    }

    private async Task SetBalancedPowerPlanAsync(RemediationAction action, CancellationToken ct)
    {
        const string balancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";
        var (exitCode, _, stderr) = await RunAsync("powercfg", $"/setactive {balancedGuid}", ct);

        if (exitCode == 0)
        {
            action.Result       = RemediationResult.Success;
            action.ResultDetail = "Power plan set to Balanced.";
        }
        else
        {
            action.Result       = RemediationResult.Failed;
            action.ResultDetail = $"powercfg exited {exitCode}: {stderr}".Trim();
        }
    }

    private async Task FlushDnsAsync(RemediationAction action, CancellationToken ct)
    {
        var (exitCode, _, stderr) = await RunAsync("ipconfig", "/flushdns", ct);

        if (exitCode == 0)
        {
            action.Result       = RemediationResult.Success;
            action.ResultDetail = "DNS resolver cache flushed.";
        }
        else
        {
            action.Result       = RemediationResult.Failed;
            action.ResultDetail = $"ipconfig /flushdns exited {exitCode}: {stderr}".Trim();
        }
    }

    private async Task RestartNetworkServicesAsync(
        RemediationAction action, CancellationToken ct)
    {
        var svcs      = new[] { "Dnscache", "NlaSvc", "WlanSvc" };
        var restarted = new List<string>();
        var failed    = new List<string>();

        foreach (var svc in svcs)
        {
            ct.ThrowIfCancellationRequested();
            var (stopCode, _, _) = await RunAsync("net", $"stop {svc} /y", ct);
            await Task.Delay(500, ct);
            var (startCode, _, startErr) = await RunAsync("net", $"start {svc}", ct);

            if (startCode == 0)
                restarted.Add(svc);
            else
                failed.Add($"{svc} ({startErr.Trim()})");
        }

        action.Result = failed.Count == 0
            ? RemediationResult.Success
            : restarted.Count > 0
                ? RemediationResult.PartialSuccess
                : RemediationResult.Failed;

        action.ResultDetail = restarted.Count > 0
            ? $"Restarted: {string.Join(", ", restarted)}."
              + (failed.Count > 0 ? $" Failed: {string.Join(", ", failed)}." : string.Empty)
            : $"All service restarts failed: {string.Join(", ", failed)}.";
    }

    private static Task OpenStartupManagerAsync(RemediationAction action)
    {
        try
        {
            Process.Start(new ProcessStartInfo("taskmgr.exe", "/0 /startup")
            {
                UseShellExecute = true
            });
            action.Result       = RemediationResult.Success;
            action.ResultDetail = "Startup manager opened — review and disable high-impact items.";
        }
        catch (Exception ex)
        {
            action.Result       = RemediationResult.Failed;
            action.ResultDetail = $"Could not open Task Manager: {ex.Message}";
        }
        return Task.CompletedTask;
    }

    private static Task LaunchVantageUpdateAsync(RemediationAction action)
    {
        try
        {
            Process.Start(new ProcessStartInfo(
                "explorer.exe", "lenovo-vantage3:SystemUpdate")
            { UseShellExecute = true });
            action.Result       = RemediationResult.Success;
            action.ResultDetail = "Lenovo Vantage update flow launched.";
        }
        catch
        {
            action.Result       = RemediationResult.PartialSuccess;
            action.ResultDetail =
                "Could not auto-launch Vantage. Open Lenovo Vantage and check for updates manually.";
        }
        return Task.CompletedTask;
    }

    private static Task UnknownActionAsync(RemediationAction action)
    {
        action.Result       = RemediationResult.Skipped;
        action.ResultDetail = $"Unknown action id: {action.ActionId}";
        return Task.CompletedTask;
    }

    // ── Shell helper with exit-code capture ───────────────────────────────────

    private static async Task<(int ExitCode, string Stdout, string Stderr)>
        RunAsync(string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {exe}");

        await p.WaitForExitAsync(ct);
        var stdout = await p.StandardOutput.ReadToEndAsync(ct);
        var stderr = await p.StandardError.ReadToEndAsync(ct);
        return (p.ExitCode, stdout, stderr);
    }
}

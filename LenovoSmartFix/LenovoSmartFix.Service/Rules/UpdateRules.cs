using LenovoSmartFix.Core.Models;
using LenovoSmartFix.Core.Rules;

namespace LenovoSmartFix.Service.Rules;

/// <summary>
/// Update-related rules. A rule only triggers when the state is definitively
/// UpdateAvailable or Critical — Unknown state means collection failed or no
/// catalog is available, and must not be treated as "needs update".
/// </summary>
public static class UpdateRules
{
    public static IReadOnlyList<Func<RuleContext, RuleResult>> All(_) =>
        new List<Func<RuleContext, RuleResult>>
        {
            OutdatedBios,
            OutdatedCriticalDrivers,
            OutdatedLenovoUtilities,
            PendingWindowsUpdates,
        };

    private static RuleResult OutdatedBios(RuleContext ctx)
    {
        var bios     = ctx.Updates.Bios;
        // Only trigger on confirmed available/critical — never on Unknown
        var triggered = bios.State is UpdateState.UpdateAvailable or UpdateState.Critical;

        return new RuleResult
        {
            RuleId             = "UPD-001",
            RuleName           = "Outdated BIOS",
            Triggered          = triggered,
            Severity           = bios.State == UpdateState.Critical
                ? RuleSeverity.Critical : RuleSeverity.High,
            RecommendedPath    = DiagnosisPath.AutoResolve,
            TriggeredCondition = $"BIOS state={bios.State}",
            Evidence           = triggered
                ? new List<string>
                  { $"BIOS {bios.CurrentVersion} installed; {bios.RecommendedVersion} available" }
                : new List<string>(),
            UserFacingMessage  =
                "A BIOS update is available. Keeping BIOS current improves stability and security."
        };
    }

    private static RuleResult OutdatedCriticalDrivers(RuleContext ctx)
    {
        var outdated = ctx.Updates.Drivers
            .Where(d => d.IsCritical
                     && d.State is UpdateState.UpdateAvailable or UpdateState.Critical)
            .ToList();
        var triggered = outdated.Any();

        return new RuleResult
        {
            RuleId             = "UPD-002",
            RuleName           = "Outdated Critical Drivers",
            Triggered          = triggered,
            Severity           = RuleSeverity.High,
            RecommendedPath    = DiagnosisPath.AutoResolve,
            TriggeredCondition = $"OutdatedCriticalDrivers={outdated.Count}",
            Evidence           = triggered
                ? outdated.Select(d =>
                    $"{d.ComponentName}: {d.CurrentVersion} → {d.RecommendedVersion}").ToList()
                : new List<string>(),
            UserFacingMessage  =
                $"{outdated.Count} critical driver update(s) available."
        };
    }

    private static RuleResult OutdatedLenovoUtilities(RuleContext ctx)
    {
        var outdated = ctx.Updates.LenovoUtilities
            .Where(u => u.State is UpdateState.UpdateAvailable or UpdateState.Critical)
            .ToList();
        var triggered = outdated.Any();

        return new RuleResult
        {
            RuleId             = "UPD-003",
            RuleName           = "Outdated Lenovo Utilities",
            Triggered          = triggered,
            Severity           = RuleSeverity.Warning,
            RecommendedPath    = DiagnosisPath.AutoResolve,
            TriggeredCondition = $"OutdatedLenovoUtilities={outdated.Count}",
            Evidence           = triggered
                ? outdated.Select(u => $"{u.ComponentName}: {u.CurrentVersion}").ToList()
                : new List<string>(),
            UserFacingMessage  = "Some Lenovo utilities are out of date."
        };
    }

    private static RuleResult PendingWindowsUpdates(RuleContext ctx)
    {
        // Do not trigger when state is Unknown (WUA query failed)
        var triggered = ctx.Updates.WindowsUpdateState
            is UpdateState.UpdateAvailable or UpdateState.Critical;

        return new RuleResult
        {
            RuleId             = "UPD-004",
            RuleName           = "Pending Windows Updates",
            Triggered          = triggered,
            Severity           = ctx.Updates.WindowsUpdateState == UpdateState.Critical
                ? RuleSeverity.High : RuleSeverity.Warning,
            RecommendedPath    = DiagnosisPath.GuidedResolution,
            TriggeredCondition =
                $"WindowsUpdateState={ctx.Updates.WindowsUpdateState}, Pending={ctx.Updates.PendingWindowsUpdates}",
            Evidence           = triggered
                ? new List<string>
                  { $"{ctx.Updates.PendingWindowsUpdates} Windows update(s) pending" }
                : new List<string>(),
            UserFacingMessage  =
                "Windows updates are waiting to be installed."
        };
    }
}

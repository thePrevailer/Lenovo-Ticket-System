using LenovoSmartFix.Core.Models;
using LenovoSmartFix.Core.Rules;

namespace LenovoSmartFix.Service.Rules;

public static class BatteryRules
{
    public static IReadOnlyList<Func<RuleContext, RuleResult>> All(ThresholdOptions t) =>
        new List<Func<RuleContext, RuleResult>>
        {
            ctx => PoorBatteryHealth(ctx, t),
            ctx => HighBackgroundLoadOnBattery(ctx, t),
            ctx => NonRecommendedPowerPlan(ctx),
        };

    private static RuleResult PoorBatteryHealth(RuleContext ctx, ThresholdOptions t)
    {
        var h = ctx.Health;
        var critical = h.BatteryHealthPercent <= t.BatteryHealthCriticalPercent;
        var warning = h.BatteryHealthPercent <= t.BatteryHealthWarningPercent;
        var triggered = warning && h.BatteryHealthPercent > 0;

        return new RuleResult
        {
            RuleId = "BATT-001",
            RuleName = "Poor Battery Health",
            Triggered = triggered,
            Severity = critical ? RuleSeverity.Critical : RuleSeverity.High,
            RecommendedPath = critical ? DiagnosisPath.Escalate : DiagnosisPath.GuidedResolution,
            TriggeredCondition = $"BatteryHealth={h.BatteryHealthPercent}%",
            Evidence = triggered
                ? new List<string>
                  {
                      $"Battery health at {h.BatteryHealthPercent}% (cycle count: {h.BatteryCycleCount})"
                  }
                : new List<string>(),
            UserFacingMessage = critical
                ? "Your battery health is critically low. Hardware replacement may be needed."
                : "Your battery health has declined and may need attention soon."
        };
    }

    private static RuleResult HighBackgroundLoadOnBattery(RuleContext ctx, ThresholdOptions t)
    {
        var h = ctx.Health;
        var triggered = !h.IsOnAcPower
            && h.HighImpactBackgroundProcessCount > 3
            && h.CpuLoadPercent >= 50;

        return new RuleResult
        {
            RuleId = "BATT-002",
            RuleName = "High Background Load on Battery",
            Triggered = triggered,
            Severity = RuleSeverity.Warning,
            RecommendedPath = DiagnosisPath.AutoResolve,
            TriggeredCondition =
                $"OnBattery=true, HighImpactProcesses={h.HighImpactBackgroundProcessCount}",
            Evidence = triggered
                ? new List<string>
                  {
                      $"{h.HighImpactBackgroundProcessCount} high-impact processes running on battery",
                      $"CPU at {h.CpuLoadPercent:F0}%"
                  }
                : new List<string>(),
            UserFacingMessage =
                "Background apps are draining your battery faster than normal."
        };
    }

    private static RuleResult NonRecommendedPowerPlan(RuleContext ctx)
    {
        var h = ctx.Health;
        var triggered = !h.IsRecommendedPowerPlan;

        return new RuleResult
        {
            RuleId = "BATT-003",
            RuleName = "Non-Recommended Power Plan",
            Triggered = triggered,
            Severity = RuleSeverity.Info,
            RecommendedPath = DiagnosisPath.AutoResolve,
            TriggeredCondition = $"PowerPlan={h.PowerPlanName}",
            Evidence = triggered
                ? new List<string> { $"Active power plan: '{h.PowerPlanName}'" }
                : new List<string>(),
            UserFacingMessage =
                $"You are using the '{h.PowerPlanName}' power plan. Switching to Balanced may improve battery life."
        };
    }
}

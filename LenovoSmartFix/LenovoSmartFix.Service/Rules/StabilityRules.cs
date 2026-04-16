using LenovoSmartFix.Core.Models;
using LenovoSmartFix.Core.Rules;

namespace LenovoSmartFix.Service.Rules;

public static class StabilityRules
{
    public static IReadOnlyList<Func<RuleContext, RuleResult>> All(ThresholdOptions t) =>
        new List<Func<RuleContext, RuleResult>>
        {
            ctx => RepeatedAppCrashes(ctx, t),
            ctx => SystemCrashes(ctx),
            ctx => ThermalThrottling(ctx),
            ctx => RecurringAfterPriorEscalation(ctx),
        };

    private static RuleResult RepeatedAppCrashes(RuleContext ctx, ThresholdOptions t)
    {
        var h = ctx.Health;
        var triggered = h.AppCrashesLast7Days >= t.AppCrashesWarningCount;

        return new RuleResult
        {
            RuleId = "STAB-001",
            RuleName = "Repeated App Crashes",
            Triggered = triggered,
            Severity = RuleSeverity.High,
            RecommendedPath = DiagnosisPath.GuidedResolution,
            TriggeredCondition = $"AppCrashes={h.AppCrashesLast7Days} in 7 days",
            Evidence = triggered
                ? new List<string>
                  {
                      $"{h.AppCrashesLast7Days} app crashes in the last 7 days"
                  }
                  .Concat(h.RecentCrashSignatures.Take(3).Select(s => $"  - {s}"))
                  .ToList()
                : new List<string>(),
            UserFacingMessage =
                $"SmartFix detected {h.AppCrashesLast7Days} app crashes in the past week."
        };
    }

    private static RuleResult SystemCrashes(RuleContext ctx)
    {
        var h = ctx.Health;
        var triggered = h.SystemCrashesLast7Days >= 1;

        return new RuleResult
        {
            RuleId = "STAB-002",
            RuleName = "System Crash / Unexpected Restart",
            Triggered = triggered,
            Severity = triggered && h.SystemCrashesLast7Days >= 3
                ? RuleSeverity.Critical : RuleSeverity.High,
            RecommendedPath = triggered && h.SystemCrashesLast7Days >= 3
                ? DiagnosisPath.Escalate : DiagnosisPath.GuidedResolution,
            TriggeredCondition = $"SystemCrashes={h.SystemCrashesLast7Days}",
            Evidence = triggered
                ? new List<string>
                  { $"{h.SystemCrashesLast7Days} unexpected system restart(s) in 7 days" }
                : new List<string>(),
            UserFacingMessage =
                h.SystemCrashesLast7Days >= 3
                ? "Your device has crashed multiple times recently. This likely requires support."
                : "Your device had an unexpected restart recently."
        };
    }

    private static RuleResult ThermalThrottling(RuleContext ctx)
    {
        var h = ctx.Health;
        var triggered = h.ThermalThrottlingDetected;

        return new RuleResult
        {
            RuleId = "STAB-003",
            RuleName = "Thermal Throttling Detected",
            Triggered = triggered,
            Severity = RuleSeverity.High,
            RecommendedPath = DiagnosisPath.GuidedResolution,
            TriggeredCondition = $"ThermalThrottling=true, CpuTemp={h.CpuTemperatureCelsius:F0}°C",
            Evidence = triggered
                ? new List<string>
                  { $"CPU temperature {h.CpuTemperatureCelsius:F0}°C — throttling active" }
                : new List<string>(),
            UserFacingMessage =
                "Your device is running hot and reducing performance to cool down."
        };
    }

    private static RuleResult RecurringAfterPriorEscalation(RuleContext ctx)
    {
        var triggered = ctx.PriorEscalationCount >= 2;

        return new RuleResult
        {
            RuleId = "STAB-004",
            RuleName = "Recurring Issue After Prior Escalation",
            Triggered = triggered,
            Severity = RuleSeverity.Critical,
            RecommendedPath = DiagnosisPath.Escalate,
            TriggeredCondition = $"PriorEscalations={ctx.PriorEscalationCount}",
            Evidence = triggered
                ? new List<string>
                  { $"This issue has been escalated {ctx.PriorEscalationCount} times before" }
                : new List<string>(),
            UserFacingMessage =
                "This issue has come back after previous attempts to fix it. Lenovo Support should investigate."
        };
    }
}

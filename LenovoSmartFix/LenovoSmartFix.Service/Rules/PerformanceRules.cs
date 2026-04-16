using LenovoSmartFix.Core.Models;
using LenovoSmartFix.Core.Rules;

namespace LenovoSmartFix.Service.Rules;

public static class PerformanceRules
{
    public static IReadOnlyList<Func<RuleContext, RuleResult>> All(ThresholdOptions t) =>
        new List<Func<RuleContext, RuleResult>>
        {
            ctx => HighStartupLoad(ctx, t),
            ctx => SustainedHighCpu(ctx, t),
            ctx => LowFreeStorage(ctx, t),
            ctx => HighRamPressure(ctx, t),
        };

    private static RuleResult HighStartupLoad(RuleContext ctx, ThresholdOptions t)
    {
        var h = ctx.Health;
        var triggered = h.StartupItemCount >= t.StartupItemsWarningCount
            && h.CpuLoadPercent >= t.CpuLoadWarningPercent;

        return new RuleResult
        {
            RuleId = "PERF-001",
            RuleName = "High Startup Load + CPU",
            Triggered = triggered,
            Severity = RuleSeverity.High,
            RecommendedPath = DiagnosisPath.AutoResolve,
            TriggeredCondition = $"StartupItems={h.StartupItemCount}, CPU={h.CpuLoadPercent:F0}%",
            Evidence = triggered
                ? new List<string>
                  {
                      $"{h.StartupItemCount} startup items detected",
                      $"CPU running at {h.CpuLoadPercent:F0}%"
                  }
                : new List<string>(),
            UserFacingMessage =
                "Your device is starting too many programs automatically, which is slowing it down."
        };
    }

    private static RuleResult SustainedHighCpu(RuleContext ctx, ThresholdOptions t)
    {
        var h = ctx.Health;
        var triggered = h.CpuLoadPercent >= t.CpuLoadWarningPercent
            && !h.ThermalThrottlingDetected;

        return new RuleResult
        {
            RuleId = "PERF-002",
            RuleName = "Sustained High CPU (non-thermal)",
            Triggered = triggered,
            Severity = RuleSeverity.Warning,
            RecommendedPath = DiagnosisPath.GuidedResolution,
            TriggeredCondition = $"CPU={h.CpuLoadPercent:F0}%",
            Evidence = triggered
                ? new List<string> { $"CPU at {h.CpuLoadPercent:F0}% with no thermal throttling" }
                : new List<string>(),
            UserFacingMessage =
                "A background program is consuming significant CPU resources."
        };
    }

    private static RuleResult LowFreeStorage(RuleContext ctx, ThresholdOptions t)
    {
        var h = ctx.Health;
        var critical = h.DiskUsedPercent >= t.DiskUsedCriticalPercent;
        var warning = h.DiskUsedPercent >= t.DiskUsedWarningPercent;
        var triggered = warning;

        return new RuleResult
        {
            RuleId = "PERF-003",
            RuleName = "Low Free Storage",
            Triggered = triggered,
            Severity = critical ? RuleSeverity.Critical : RuleSeverity.High,
            RecommendedPath = critical ? DiagnosisPath.GuidedResolution : DiagnosisPath.AutoResolve,
            TriggeredCondition = $"DiskUsed={h.DiskUsedPercent:F1}%",
            Evidence = triggered
                ? new List<string>
                  {
                      $"Disk {h.DiskUsedPercent:F1}% full ({h.DiskFreeBytes / 1_073_741_824.0:F1} GB free)"
                  }
                : new List<string>(),
            UserFacingMessage =
                $"Your drive is {h.DiskUsedPercent:F0}% full. Low disk space can cause slowdowns."
        };
    }

    private static RuleResult HighRamPressure(RuleContext ctx, ThresholdOptions t)
    {
        var h = ctx.Health;
        var triggered = h.RamUsedPercent >= t.RamUsedWarningPercent
            || h.PageFaultsPerSec > 200;

        return new RuleResult
        {
            RuleId = "PERF-004",
            RuleName = "High RAM Pressure",
            Triggered = triggered,
            Severity = h.RamUsedPercent >= t.RamUsedCriticalPercent
                ? RuleSeverity.High : RuleSeverity.Warning,
            RecommendedPath = DiagnosisPath.GuidedResolution,
            TriggeredCondition = $"RAM={h.RamUsedPercent:F1}%, PageFaults={h.PageFaultsPerSec}/s",
            Evidence = triggered
                ? new List<string>
                  {
                      $"RAM at {h.RamUsedPercent:F0}% used",
                      $"Page faults: {h.PageFaultsPerSec}/sec"
                  }
                : new List<string>(),
            UserFacingMessage =
                "Your device is low on memory. Some apps may run slowly or become unresponsive."
        };
    }
}

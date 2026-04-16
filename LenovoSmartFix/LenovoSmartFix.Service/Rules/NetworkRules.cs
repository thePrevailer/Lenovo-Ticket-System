using LenovoSmartFix.Core.Models;
using LenovoSmartFix.Core.Rules;

namespace LenovoSmartFix.Service.Rules;

public static class NetworkRules
{
    public static IReadOnlyList<Func<RuleContext, RuleResult>> All(ThresholdOptions t) =>
        new List<Func<RuleContext, RuleResult>>
        {
            ctx => UnstableWifi(ctx, t),
            ctx => OutdatedNetworkDriver(ctx),
        };

    private static RuleResult UnstableWifi(RuleContext ctx, ThresholdOptions t)
    {
        var h = ctx.Health;
        var triggered = h.WifiAdapterPresent
            && h.WifiReconnectsLast24h >= t.WifiReconnectWarningCount;

        return new RuleResult
        {
            RuleId = "NET-001",
            RuleName = "Unstable Wi-Fi Connection",
            Triggered = triggered,
            Severity = RuleSeverity.High,
            RecommendedPath = DiagnosisPath.GuidedResolution,
            TriggeredCondition = $"WifiReconnects={h.WifiReconnectsLast24h} in last 24h",
            Evidence = triggered
                ? new List<string>
                  {
                      $"Wi-Fi reconnected {h.WifiReconnectsLast24h} times in the last 24 hours",
                      $"Adapter: {h.WifiAdapterName}"
                  }
                : new List<string>(),
            UserFacingMessage =
                $"Your Wi-Fi connection has dropped {h.WifiReconnectsLast24h} times in the last 24 hours."
        };
    }

    private static RuleResult OutdatedNetworkDriver(RuleContext ctx)
    {
        var h = ctx.Health;
        var triggered = h.WifiAdapterPresent && !h.WifiDriverUpToDate;

        return new RuleResult
        {
            RuleId = "NET-002",
            RuleName = "Outdated Network Adapter Driver",
            Triggered = triggered,
            Severity = RuleSeverity.Warning,
            RecommendedPath = DiagnosisPath.AutoResolve,
            TriggeredCondition = "WifiDriverUpToDate=false",
            Evidence = triggered
                ? new List<string> { $"Network adapter driver out of date: {h.WifiAdapterName}" }
                : new List<string>(),
            UserFacingMessage =
                "Your Wi-Fi adapter driver is out of date, which may cause connection issues."
        };
    }
}

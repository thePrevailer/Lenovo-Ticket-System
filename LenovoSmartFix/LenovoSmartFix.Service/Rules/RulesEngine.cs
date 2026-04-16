using LenovoSmartFix.Core.Interfaces;
using LenovoSmartFix.Core.Models;
using LenovoSmartFix.Core.Rules;
using Microsoft.Extensions.Options;

namespace LenovoSmartFix.Service.Rules;

/// <summary>
/// Evaluates all rule families in priority order and synthesises a DiagnosisDecision.
/// Priority: Critical hardware suspicion > outdated critical stack > software/config issues.
/// </summary>
public sealed class RulesEngine : IRuleEngine
{
    private readonly ThresholdOptions _thresholds;
    private readonly ILogger<RulesEngine> _logger;

    private readonly IReadOnlyList<IReadOnlyList<Func<RuleContext, RuleResult>>> _ruleFamilies;

    public RulesEngine(IOptions<SmartFixOptions> opts, ILogger<RulesEngine> logger)
    {
        _thresholds = opts.Value.Thresholds;
        _logger = logger;

        _ruleFamilies = new List<IReadOnlyList<Func<RuleContext, RuleResult>>>
        {
            StabilityRules.All(_thresholds),
            UpdateRules.All(_thresholds),
            PerformanceRules.All(_thresholds),
            BatteryRules.All(_thresholds),
            NetworkRules.All(_thresholds)
        };
    }

    public (DiagnosisDecision Decision, IReadOnlyList<RuleResult> TriggeredRules) Evaluate(
        RuleContext context)
    {
        var triggered = new List<RuleResult>();

        foreach (var family in _ruleFamilies)
        {
            foreach (var rule in family)
            {
                try
                {
                    var result = rule(context);
                    if (result.Triggered)
                    {
                        triggered.Add(result);
                        _logger.LogInformation(
                            "Rule triggered: {RuleId} [{Severity}] -> {Path}",
                            result.RuleId, result.Severity, result.RecommendedPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Rule evaluation threw an exception");
                }
            }
        }

        // Escalate if any critical/high-risk rule fired, or if there were prior escalations
        var decision = Synthesise(triggered, context.PriorEscalationCount);
        return (decision, triggered);
    }

    private static DiagnosisDecision Synthesise(
        IReadOnlyList<RuleResult> triggered, int priorEscalations)
    {
        if (!triggered.Any())
        {
            return new DiagnosisDecision
            {
                Path = DiagnosisPath.AutoResolve,
                Confidence = 0.9,
                RiskLevel = RiskLevel.Low,
                UserFacingReason = "Your device looks healthy. No significant issues were found.",
                TechnicalSummary = "No rules triggered."
            };
        }

        var hasCritical = triggered.Any(r => r.Severity == RuleSeverity.Critical);
        var hasHigh = triggered.Any(r => r.Severity == RuleSeverity.High);
        var anyEscalate = triggered.Any(r => r.RecommendedPath == DiagnosisPath.Escalate);

        DiagnosisPath path;
        RiskLevel risk;
        double confidence;

        if (hasCritical || anyEscalate || priorEscalations >= 2)
        {
            path = DiagnosisPath.Escalate;
            risk = RiskLevel.High;
            confidence = 0.85;
        }
        else if (hasHigh)
        {
            path = DiagnosisPath.GuidedResolution;
            risk = RiskLevel.Medium;
            confidence = 0.80;
        }
        else
        {
            path = DiagnosisPath.AutoResolve;
            risk = RiskLevel.Low;
            confidence = 0.75;
        }

        var evidence = triggered.SelectMany(r => r.Evidence).Distinct().ToList();
        var ruleIds = triggered.Select(r => r.RuleId).ToList();
        var topMessage = triggered
            .OrderByDescending(r => (int)r.Severity)
            .First().UserFacingMessage;

        return new DiagnosisDecision
        {
            Path = path,
            Confidence = confidence,
            RiskLevel = risk,
            UserFacingReason = topMessage,
            TechnicalSummary = $"{triggered.Count} rule(s) triggered: {string.Join(", ", ruleIds)}",
            TriggeredRuleIds = ruleIds,
            EvidenceItems = evidence
        };
    }
}

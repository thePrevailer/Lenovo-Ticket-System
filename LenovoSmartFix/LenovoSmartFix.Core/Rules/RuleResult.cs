using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Core.Rules;

/// <summary>
/// Output from a single diagnostic rule evaluation.
/// </summary>
public sealed class RuleResult
{
    public string RuleId { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public bool Triggered { get; init; }
    public RuleSeverity Severity { get; init; }
    public DiagnosisPath RecommendedPath { get; init; }
    public string TriggeredCondition { get; init; } = string.Empty;
    public List<string> Evidence { get; init; } = new();
    public string UserFacingMessage { get; init; } = string.Empty;
}

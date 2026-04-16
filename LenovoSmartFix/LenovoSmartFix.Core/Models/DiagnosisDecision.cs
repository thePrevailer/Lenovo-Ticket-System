namespace LenovoSmartFix.Core.Models;

public enum DiagnosisPath
{
    AutoResolve,
    GuidedResolution,
    Escalate
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

/// <summary>
/// Output of the rules engine: what path to take, why, and at what confidence.
/// </summary>
public sealed class DiagnosisDecision
{
    public DiagnosisPath Path { get; init; }
    public double Confidence { get; init; }    // 0.0 - 1.0
    public RiskLevel RiskLevel { get; init; }

    // Human-facing
    public string UserFacingReason { get; init; } = string.Empty;

    // Technical detail for support packet
    public string TechnicalSummary { get; init; } = string.Empty;

    // Triggered rule ids and the evidence each rule produced
    public List<string> TriggeredRuleIds { get; init; } = new();
    public List<string> EvidenceItems { get; init; } = new();

    public DateTimeOffset DecidedAt { get; init; } = DateTimeOffset.UtcNow;
}

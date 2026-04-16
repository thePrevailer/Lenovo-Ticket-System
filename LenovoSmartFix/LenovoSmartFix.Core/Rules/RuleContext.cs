using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Core.Rules;

/// <summary>
/// All inputs the rules engine needs to evaluate every rule family.
/// Includes optional previous scan for regression detection.
/// </summary>
public sealed class RuleContext
{
    public DeviceProfile Device { get; init; } = new();
    public HealthSnapshot Health { get; init; } = new();
    public UpdateStatus Updates { get; init; } = new();

    // Previous known-good baseline for regression comparison (may be null on first scan)
    public HealthSnapshot? PreviousHealthBaseline { get; init; }

    // Number of times the same symptom has been escalated in the last 30 days
    public int PriorEscalationCount { get; init; }
}

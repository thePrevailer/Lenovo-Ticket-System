namespace LenovoSmartFix.Core.Models;

/// <summary>
/// Structured support-ready packet containing full context for Lenovo Support or enterprise IT.
/// Designed for JSON export (machine-readable) and PDF summary (human-readable).
/// </summary>
public sealed class EscalationPacket
{
    public string PacketId { get; init; } = Guid.NewGuid().ToString();
    public string SchemaVersion { get; init; } = "1.0";

    // Primary symptom as described or selected by the user
    public string PrimarySymptom { get; init; } = string.Empty;

    public DeviceProfile DeviceProfile { get; init; } = new();
    public HealthSnapshot HealthSnapshot { get; init; } = new();
    public UpdateStatus UpdateStatus { get; init; } = new();

    public DiagnosisDecision DiagnosisDecision { get; init; } = new();

    // All remediation actions attempted in this session
    public List<RemediationAction> ActionsAttempted { get; init; } = new();

    // Overall session outcome
    public string Outcome { get; init; } = string.Empty;
    public string UnresolvedReason { get; init; } = string.Empty;

    // Privacy: redact personal fields on export unless user opts in
    public bool IsRedacted { get; init; } = true;

    public DateTimeOffset ExportedAt { get; init; } = DateTimeOffset.UtcNow;
}

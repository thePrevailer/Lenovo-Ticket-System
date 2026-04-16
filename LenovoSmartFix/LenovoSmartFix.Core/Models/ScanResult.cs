namespace LenovoSmartFix.Core.Models;

public enum ScanStatus
{
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Top-level container returned after a full SmartFix scan session.
/// </summary>
public sealed class ScanResult
{
    public string ScanId { get; init; } = Guid.NewGuid().ToString();
    public string Symptom { get; init; } = string.Empty;
    public ScanStatus Status { get; set; } = ScanStatus.Running;

    public DeviceProfile? DeviceProfile { get; set; }
    public HealthSnapshot? HealthSnapshot { get; set; }
    public UpdateStatus? UpdateStatus { get; set; }
    public DiagnosisDecision? Decision { get; set; }
    public List<RemediationAction> Actions { get; set; } = new();
    public EscalationPacket? EscalationPacket { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

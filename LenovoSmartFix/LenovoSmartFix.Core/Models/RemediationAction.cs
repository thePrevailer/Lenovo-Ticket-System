namespace LenovoSmartFix.Core.Models;

public enum RemediationSafetyLevel
{
    Safe,       // Runs automatically with no consent required
    Consent,    // Requires explicit user confirmation
    Guided      // User performs steps; service only advises
}

public enum RemediationResult
{
    Pending,
    Success,
    PartialSuccess,
    Failed,
    Skipped,
    RolledBack
}

/// <summary>
/// A single remediation step associated with a specific scan.
/// <para>
/// <see cref="ActionId"/> is the stable library code (e.g. REM-TEMP-CLEANUP).
/// <see cref="ActionInstanceId"/> is a per-scan UUID that uniquely identifies this
/// particular execution request and is used for all follow-up operations
/// (execution, consent logging, packet generation) so repeated scans never collide.
/// </para>
/// </summary>
public sealed class RemediationAction
{
    /// <summary>Stable library code — identifies what kind of action this is.</summary>
    public string ActionId { get; init; } = string.Empty;

    /// <summary>Per-scan unique identifier — used for execution and persistence lookups.</summary>
    public string ActionInstanceId { get; init; } = Guid.NewGuid().ToString();

    public string ActionName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public RemediationSafetyLevel SafetyLevel { get; init; }
    public bool ConsentRequired => SafetyLevel != RemediationSafetyLevel.Safe;
    public bool IsRollbackable { get; init; }

    // Populated after execution
    public RemediationResult Result { get; set; } = RemediationResult.Pending;
    public string? ResultDetail { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }
    public bool UserConsented { get; set; }
}

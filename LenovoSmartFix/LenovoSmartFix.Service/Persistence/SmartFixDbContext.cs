using LenovoSmartFix.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LenovoSmartFix.Service.Persistence;

public sealed class SmartFixDbContext : DbContext
{
    public SmartFixDbContext(DbContextOptions<SmartFixDbContext> options) : base(options) { }

    public DbSet<ScanRecord>        Scans         => Set<ScanRecord>();
    public DbSet<ActionRecord>      Actions       => Set<ActionRecord>();
    public DbSet<EscalationRecord>  Escalations   => Set<EscalationRecord>();
    public DbSet<ConsentRecord>     Consents      => Set<ConsentRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<ScanRecord>(e =>
        {
            e.HasKey(x => x.ScanId);
            e.HasIndex(x => x.DeviceId);
            e.HasIndex(x => x.StartedAt);
            e.Property(x => x.DeviceProfileJson).HasColumnType("TEXT");
            e.Property(x => x.HealthSnapshotJson).HasColumnType("TEXT");
            e.Property(x => x.UpdateStatusJson).HasColumnType("TEXT");
            e.Property(x => x.DecisionJson).HasColumnType("TEXT");
        });

        model.Entity<ActionRecord>(e =>
        {
            e.HasKey(x => x.ActionInstanceId);
            e.HasIndex(x => x.ScanId);
            e.HasIndex(x => x.ActionId);
        });

        model.Entity<EscalationRecord>(e =>
        {
            e.HasKey(x => x.PacketId);
            e.HasIndex(x => x.ScanId);
            e.Property(x => x.PacketJson).HasColumnType("TEXT");
        });

        model.Entity<ConsentRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ScanId);
        });
    }
}

// ── Entities ─────────────────────────────────────────────────────────────────

/// <summary>
/// Persists one scan session. All collected data is stored as JSON so the
/// domain model can evolve without requiring schema migrations for every field.
/// </summary>
public sealed class ScanRecord
{
    public string ScanId             { get; set; } = string.Empty;
    public string DeviceId           { get; set; } = string.Empty;   // denormalised for query
    public string Symptom            { get; set; } = string.Empty;
    public string Status             { get; set; } = ScanStatus.Running.ToString();
    public int    ProgressPercent    { get; set; }
    public string ProgressStep       { get; set; } = string.Empty;
    public string DeviceProfileJson  { get; set; } = string.Empty;
    public string HealthSnapshotJson { get; set; } = string.Empty;
    public string UpdateStatusJson   { get; set; } = string.Empty;
    public string DecisionJson       { get; set; } = string.Empty;
    public string? ErrorMessage      { get; set; }
    public DateTimeOffset StartedAt  { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// One remediation action instance for a specific scan.
/// ActionId is the stable library code; ActionInstanceId is the per-scan UUID.
/// </summary>
public sealed class ActionRecord
{
    public string ActionInstanceId { get; set; } = string.Empty;
    public string ActionId         { get; set; } = string.Empty;
    public string ScanId           { get; set; } = string.Empty;
    public string ActionName       { get; set; } = string.Empty;
    public string Description      { get; set; } = string.Empty;
    public string SafetyLevel      { get; set; } = string.Empty;
    public bool   IsRollbackable   { get; set; }
    public string Result           { get; set; } = RemediationResult.Pending.ToString();
    public string? ResultDetail    { get; set; }
    public bool   UserConsented    { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }
}

public sealed class EscalationRecord
{
    public string PacketId    { get; set; } = string.Empty;
    public string ScanId      { get; set; } = string.Empty;
    public string PacketJson  { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ConsentRecord
{
    public string Id         { get; set; } = Guid.NewGuid().ToString();
    public string ScanId     { get; set; } = string.Empty;
    public string ActionInstanceId { get; set; } = string.Empty;
    public bool   Consented  { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}

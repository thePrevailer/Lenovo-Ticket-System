using System.Text.Json;
using System.Text.Json.Serialization;
using LenovoSmartFix.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LenovoSmartFix.Service.Persistence;

/// <summary>
/// All reads and writes go through SQLite. No in-memory session state is kept
/// here — every operation must survive a service restart.
/// </summary>
public sealed class SmartFixRepository
{
    private readonly SmartFixDbContext _db;
    private readonly ILogger<SmartFixRepository> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public SmartFixRepository(SmartFixDbContext db, ILogger<SmartFixRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Scans ────────────────────────────────────────────────────────────────

    public async Task UpsertScanAsync(ScanResult scan, CancellationToken ct = default)
    {
        var deviceId = scan.DeviceProfile?.DeviceId ?? string.Empty;
        var existing = await _db.Scans.FindAsync(new object[] { scan.ScanId }, ct);

        if (existing is null)
        {
            _db.Scans.Add(MapToRecord(scan, deviceId));
        }
        else
        {
            // Update all mutable fields so a completed scan is fully queryable
            existing.Status             = scan.Status.ToString();
            existing.ProgressPercent    = 100;
            existing.ProgressStep       = scan.Status == ScanStatus.Completed ? "Scan complete." : string.Empty;
            existing.DeviceProfileJson  = Serialize(scan.DeviceProfile);
            existing.HealthSnapshotJson = Serialize(scan.HealthSnapshot);
            existing.UpdateStatusJson   = Serialize(scan.UpdateStatus);
            existing.DecisionJson       = Serialize(scan.Decision);
            existing.ErrorMessage       = scan.ErrorMessage;
            existing.CompletedAt        = scan.CompletedAt;
            if (!string.IsNullOrEmpty(deviceId)) existing.DeviceId = deviceId;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateScanProgressAsync(
        string scanId, int percent, string step, CancellationToken ct = default)
    {
        var rec = await _db.Scans.FindAsync(new object[] { scanId }, ct);
        if (rec is null) return;
        rec.ProgressPercent = percent;
        rec.ProgressStep    = step;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ScanResult?> GetScanByIdAsync(
        string scanId, CancellationToken ct = default)
    {
        var rec = await _db.Scans.FindAsync(new object[] { scanId }, ct);
        if (rec is null) return null;

        var actions = await _db.Actions
            .Where(a => a.ScanId == scanId)
            .ToListAsync(ct);

        return MapToDomain(rec, actions);
    }

    public async Task<(ScanStatus Status, int Percent, string Step, string? Error)>
        GetScanProgressAsync(string scanId, CancellationToken ct = default)
    {
        var rec = await _db.Scans.FindAsync(new object[] { scanId }, ct);
        if (rec is null) return (ScanStatus.Failed, 0, string.Empty, "Scan not found");
        return (
            Enum.Parse<ScanStatus>(rec.Status),
            rec.ProgressPercent,
            rec.ProgressStep,
            rec.ErrorMessage
        );
    }

    // ── Actions ──────────────────────────────────────────────────────────────

    public async Task SaveActionsAsync(
        string scanId, IEnumerable<RemediationAction> actions, CancellationToken ct = default)
    {
        foreach (var a in actions)
        {
            var existing = await _db.Actions.FindAsync(
                new object[] { a.ActionInstanceId }, ct);
            if (existing is null)
            {
                _db.Actions.Add(MapActionToRecord(scanId, a));
            }
            else
            {
                existing.Result        = a.Result.ToString();
                existing.ResultDetail  = a.ResultDetail;
                existing.UserConsented = a.UserConsented;
                existing.ExecutedAt    = a.ExecutedAt;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<RemediationAction?> GetActionByInstanceIdAsync(
        string actionInstanceId, CancellationToken ct = default)
    {
        var rec = await _db.Actions.FindAsync(
            new object[] { actionInstanceId }, ct);
        return rec is null ? null : MapActionToDomain(rec);
    }

    public async Task<List<RemediationAction>> GetActionsByScanIdAsync(
        string scanId, CancellationToken ct = default)
    {
        var records = await _db.Actions
            .Where(a => a.ScanId == scanId)
            .ToListAsync(ct);
        return records.Select(MapActionToDomain).ToList();
    }

    public async Task UpdateActionAsync(
        RemediationAction action, CancellationToken ct = default)
    {
        var rec = await _db.Actions.FindAsync(
            new object[] { action.ActionInstanceId }, ct);
        if (rec is null) return;
        rec.Result        = action.Result.ToString();
        rec.ResultDetail  = action.ResultDetail;
        rec.UserConsented = action.UserConsented;
        rec.ExecutedAt    = action.ExecutedAt;
        await _db.SaveChangesAsync(ct);
    }

    // ── Consent ───────────────────────────────────────────────────────────────

    public async Task SaveConsentAsync(
        string scanId, string actionInstanceId, bool consented, CancellationToken ct = default)
    {
        _db.Consents.Add(new ConsentRecord
        {
            ScanId           = scanId,
            ActionInstanceId = actionInstanceId,
            Consented        = consented
        });
        await _db.SaveChangesAsync(ct);
    }

    // ── Escalations ──────────────────────────────────────────────────────────

    public async Task SaveEscalationPacketAsync(
        string scanId, EscalationPacket packet, CancellationToken ct = default)
    {
        // Upsert so re-exporting after a restart doesn't duplicate rows
        var existing = await _db.Escalations.FindAsync(
            new object[] { packet.PacketId }, ct);
        if (existing is null)
        {
            _db.Escalations.Add(new EscalationRecord
            {
                PacketId   = packet.PacketId,
                ScanId     = scanId,
                PacketJson = Serialize(packet),
                CreatedAt  = DateTimeOffset.UtcNow
            });
        }
        else
        {
            existing.PacketJson = Serialize(packet);
        }
        await _db.SaveChangesAsync(ct);
    }

    // ── Baseline / escalation history ────────────────────────────────────────

    /// <summary>
    /// Returns the most recent completed health snapshot for this specific device,
    /// for regression comparison against the current scan.
    /// </summary>
    public async Task<HealthSnapshot?> GetLastHealthBaselineAsync(
        string deviceId, string currentScanId, CancellationToken ct = default)
    {
        var rec = await _db.Scans
            .Where(s => s.DeviceId == deviceId
                     && s.ScanId  != currentScanId
                     && s.Status  == ScanStatus.Completed.ToString())
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefaultAsync(ct);

        if (rec is null || string.IsNullOrEmpty(rec.HealthSnapshotJson))
            return null;

        return Deserialize<HealthSnapshot>(rec.HealthSnapshotJson);
    }

    /// <summary>
    /// Count escalations for this device + symptom within the retention window.
    /// </summary>
    public async Task<int> GetPriorEscalationCountAsync(
        string deviceId, string symptom, DateTimeOffset since, CancellationToken ct = default)
    {
        // Join escalations → scans to get device + symptom
        return await (
            from e in _db.Escalations
            join s in _db.Scans on e.ScanId equals s.ScanId
            where s.DeviceId == deviceId
               && s.Symptom  == symptom
               && e.CreatedAt >= since
            select e.PacketId
        ).CountAsync(ct);
    }

    // ── Housekeeping ─────────────────────────────────────────────────────────

    public async Task PurgeOldRecordsAsync(int retentionDays, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        var oldScans = await _db.Scans
            .Where(s => s.StartedAt < cutoff)
            .Select(s => s.ScanId)
            .ToListAsync(ct);

        if (!oldScans.Any()) return;

        _db.Scans.RemoveRange(_db.Scans.Where(s => oldScans.Contains(s.ScanId)));
        _db.Actions.RemoveRange(_db.Actions.Where(a => oldScans.Contains(a.ScanId)));
        _db.Consents.RemoveRange(_db.Consents.Where(c => oldScans.Contains(c.ScanId)));
        _db.Escalations.RemoveRange(_db.Escalations.Where(e => oldScans.Contains(e.ScanId)));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Purged {Count} scans older than {Days} days", oldScans.Count, retentionDays);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static ScanRecord MapToRecord(ScanResult s, string deviceId) => new()
    {
        ScanId             = s.ScanId,
        DeviceId           = deviceId,
        Symptom            = s.Symptom,
        Status             = s.Status.ToString(),
        DeviceProfileJson  = Serialize(s.DeviceProfile),
        HealthSnapshotJson = Serialize(s.HealthSnapshot),
        UpdateStatusJson   = Serialize(s.UpdateStatus),
        DecisionJson       = Serialize(s.Decision),
        ErrorMessage       = s.ErrorMessage,
        StartedAt          = s.StartedAt,
        CompletedAt        = s.CompletedAt
    };

    private static ScanResult MapToDomain(ScanRecord r, List<ActionRecord> actions) => new()
    {
        ScanId         = r.ScanId,
        Symptom        = r.Symptom,
        Status         = Enum.Parse<ScanStatus>(r.Status),
        DeviceProfile  = Deserialize<DeviceProfile>(r.DeviceProfileJson),
        HealthSnapshot = Deserialize<HealthSnapshot>(r.HealthSnapshotJson),
        UpdateStatus   = Deserialize<UpdateStatus>(r.UpdateStatusJson),
        Decision       = Deserialize<DiagnosisDecision>(r.DecisionJson),
        Actions        = actions.Select(MapActionToDomain).ToList(),
        ErrorMessage   = r.ErrorMessage,
        StartedAt      = r.StartedAt,
        CompletedAt    = r.CompletedAt
    };

    private static ActionRecord MapActionToRecord(string scanId, RemediationAction a) => new()
    {
        ActionInstanceId = a.ActionInstanceId,
        ActionId         = a.ActionId,
        ScanId           = scanId,
        ActionName       = a.ActionName,
        Description      = a.Description,
        SafetyLevel      = a.SafetyLevel.ToString(),
        IsRollbackable   = a.IsRollbackable,
        Result           = a.Result.ToString(),
        ResultDetail     = a.ResultDetail,
        UserConsented    = a.UserConsented,
        ExecutedAt       = a.ExecutedAt
    };

    private static RemediationAction MapActionToDomain(ActionRecord r) => new()
    {
        ActionInstanceId = r.ActionInstanceId,
        ActionId         = r.ActionId,
        ActionName       = r.ActionName,
        Description      = r.Description,
        SafetyLevel      = Enum.Parse<RemediationSafetyLevel>(r.SafetyLevel),
        IsRollbackable   = r.IsRollbackable,
        Result           = Enum.Parse<RemediationResult>(r.Result),
        ResultDetail     = r.ResultDetail,
        UserConsented    = r.UserConsented,
        ExecutedAt       = r.ExecutedAt
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Serialize<T>(T? obj) =>
        obj is null ? string.Empty : JsonSerializer.Serialize(obj, JsonOpts);

    private static T? Deserialize<T>(string json) =>
        string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOpts);
}

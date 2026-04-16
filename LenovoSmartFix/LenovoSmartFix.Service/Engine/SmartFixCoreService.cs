using LenovoSmartFix.Core.IPC;
using LenovoSmartFix.Core.Interfaces;
using LenovoSmartFix.Core.Models;
using LenovoSmartFix.Core.Rules;
using LenovoSmartFix.Service.Escalation;
using LenovoSmartFix.Service.Persistence;
using LenovoSmartFix.Service.Remediation;
using LenovoSmartFix.Service.Rules;
using Microsoft.Extensions.Options;

namespace LenovoSmartFix.Service.Engine;

/// <summary>
/// Orchestrates the full scan-diagnose-remediate-escalate pipeline.
///
/// Design invariants:
/// - No in-memory session state. Every operation loads from SQLite so the
///   service can restart without losing scan or action context.
/// - StartScanAsync creates the DB record and fires background work; it returns
///   the scanId immediately so the caller can poll via GetScanStatusAsync.
/// - All follow-up calls (remediation, packet, export) accept scanId and reload
///   from persistence rather than expecting prior session objects.
/// </summary>
public sealed class SmartFixCoreService : ISmartFixService
{
    private readonly IDeviceCollector _deviceCollector;
    private readonly IHealthCollector _healthCollector;
    private readonly IUpdateValidator _updateValidator;
    private readonly IRuleEngine _ruleEngine;
    private readonly RemediationExecutor _remediator;
    private readonly EscalationPacketBuilder _packetBuilder;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SmartFixOptions _opts;
    private readonly ILogger<SmartFixCoreService> _logger;

    public SmartFixCoreService(
        IDeviceCollector deviceCollector,
        IHealthCollector healthCollector,
        IUpdateValidator updateValidator,
        IRuleEngine ruleEngine,
        RemediationExecutor remediator,
        EscalationPacketBuilder packetBuilder,
        IServiceScopeFactory scopeFactory,
        IOptions<SmartFixOptions> opts,
        ILogger<SmartFixCoreService> logger)
    {
        _deviceCollector = deviceCollector;
        _healthCollector = healthCollector;
        _updateValidator = updateValidator;
        _ruleEngine      = ruleEngine;
        _remediator      = remediator;
        _packetBuilder   = packetBuilder;
        _scopeFactory    = scopeFactory;
        _opts            = opts.Value;
        _logger          = logger;
    }

    // ── Start scan (non-blocking) ────────────────────────────────────────────

    public async Task<string> StartScanAsync(string symptom, CancellationToken ct = default)
    {
        var scan = new ScanResult { Symptom = symptom, Status = ScanStatus.Running };

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SmartFixRepository>();
        await repo.UpsertScanAsync(scan, ct);

        _ = Task.Run(() => RunScanPipelineAsync(scan.ScanId, symptom), CancellationToken.None);

        _logger.LogInformation("Scan {ScanId} started for symptom '{Symptom}'", scan.ScanId, symptom);
        return scan.ScanId;
    }

    // ── Poll scan status ─────────────────────────────────────────────────────

    public async Task<ScanStatusDto> GetScanStatusAsync(
        string scanId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SmartFixRepository>();

        var (status, percent, step, error) = await repo.GetScanProgressAsync(scanId, ct);

        var dto = new ScanStatusDto
        {
            ScanId          = scanId,
            Status          = status,
            ProgressPercent = percent,
            ProgressStep    = step,
            ErrorMessage    = error
        };

        if (status is ScanStatus.Completed or ScanStatus.Failed)
            dto.Result = await repo.GetScanByIdAsync(scanId, ct);

        return dto;
    }

    // ── Execute remediation ──────────────────────────────────────────────────

    public async Task<RemediationAction> ExecuteRemediationAsync(
        string scanId,
        string actionInstanceId,
        bool userConsented,
        CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SmartFixRepository>();

        var action = await repo.GetActionByInstanceIdAsync(actionInstanceId, ct)
            ?? throw new InvalidOperationException(
                $"Action instance {actionInstanceId} not found for scan {scanId}");

        await _remediator.ExecuteAsync(action, userConsented, ct);

        await repo.UpdateActionAsync(action, ct);
        await repo.SaveConsentAsync(scanId, actionInstanceId, userConsented, ct);

        return action;
    }

    // ── Build escalation packet ──────────────────────────────────────────────

    public async Task<EscalationPacket> BuildEscalationPacketAsync(
        string scanId, bool redact = true, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SmartFixRepository>();

        var scan = await repo.GetScanByIdAsync(scanId, ct)
            ?? throw new InvalidOperationException($"Scan {scanId} not found");

        if (scan.DeviceProfile is null || scan.HealthSnapshot is null
            || scan.UpdateStatus is null || scan.Decision is null)
            throw new InvalidOperationException(
                $"Scan {scanId} is incomplete — cannot build escalation packet");

        var packet = _packetBuilder.Build(
            scan.Symptom,
            scan.DeviceProfile,
            scan.HealthSnapshot,
            scan.UpdateStatus,
            scan.Decision,
            scan.Actions,
            redact);

        await repo.SaveEscalationPacketAsync(scanId, packet, ct);
        return packet;
    }

    // ── Export ───────────────────────────────────────────────────────────────

    public async Task<(string JsonPath, string? PdfPath)> ExportEscalationPacketAsync(
        EscalationPacket packet,
        string exportDirectory,
        bool includePdf = true,
        CancellationToken ct = default)
    {
        var jsonPath = await _packetBuilder.ExportJsonAsync(packet, exportDirectory, ct);
        string? pdfPath = includePdf
            ? await _packetBuilder.ExportPdfAsync(packet, exportDirectory, ct)
            : null;
        return (jsonPath, pdfPath);
    }

    // ── Background scan pipeline ─────────────────────────────────────────────

    private async Task RunScanPipelineAsync(string scanId, string symptom)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SmartFixRepository>();

        var scan = new ScanResult
        {
            ScanId  = scanId,
            Symptom = symptom,
            Status  = ScanStatus.Running
        };

        async Task Report(int pct, string step)
        {
            await repo.UpdateScanProgressAsync(scanId, pct, step);
            _logger.LogDebug("Scan {ScanId}: [{Pct}%] {Step}", scanId, pct, step);
        }

        try
        {
            await Report(10, "Identifying device");
            scan.DeviceProfile = await _deviceCollector.CollectAsync();

            await Report(30, "Collecting health metrics");
            scan.HealthSnapshot = await _healthCollector.CollectAsync();

            await Report(50, "Validating software stack");
            scan.UpdateStatus = await _updateValidator.ValidateAsync(scan.DeviceProfile);

            await Report(65, "Running diagnostic rules");
            var baseline = await repo.GetLastHealthBaselineAsync(
                scan.DeviceProfile.DeviceId, scanId);
            var priorEscalations = await repo.GetPriorEscalationCountAsync(
                scan.DeviceProfile.DeviceId, symptom,
                DateTimeOffset.UtcNow.AddDays(-_opts.ScanHistoryRetentionDays));

            var ctx = new RuleContext
            {
                Device                = scan.DeviceProfile,
                Health                = scan.HealthSnapshot,
                Updates               = scan.UpdateStatus,
                PreviousHealthBaseline = baseline,
                PriorEscalationCount  = priorEscalations
            };

            var (decision, triggeredRules) = _ruleEngine.Evaluate(ctx);
            scan.Decision = decision;

            await Report(75, "Building remediation plan");
            scan.Actions = RemediationActionFactory.BuildFrom(triggeredRules, decision.Path);
            await repo.SaveActionsAsync(scanId, scan.Actions);

            await Report(88, "Applying safe automatic fixes");
            foreach (var action in scan.Actions
                .Where(a => a.SafetyLevel == RemediationSafetyLevel.Safe))
            {
                await _remediator.ExecuteAsync(action, userConsented: false);
                await repo.UpdateActionAsync(action);
            }

            scan.Status      = ScanStatus.Completed;
            scan.CompletedAt = DateTimeOffset.UtcNow;
            await Report(100, "Scan complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan {ScanId} failed", scanId);
            scan.Status       = ScanStatus.Failed;
            scan.ErrorMessage = ex.Message;
        }

        await repo.UpsertScanAsync(scan);
    }
}

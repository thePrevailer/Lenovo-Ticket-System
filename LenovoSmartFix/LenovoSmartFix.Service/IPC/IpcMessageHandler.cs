using System.Text.Json;
using System.Text.Json.Serialization;
using LenovoSmartFix.Core.IPC;
using LenovoSmartFix.Core.Interfaces;
using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Service.IPC;

/// <summary>
/// Routes IPC requests to the SmartFix service.
///
/// StartScan  → non-blocking: create record, return scanId immediately.
/// GetScanStatus → return ScanStatusDto (progress + final result when done).
/// All other commands reload from SQLite — restart-safe.
/// </summary>
public sealed class IpcMessageHandler
{
    private readonly ISmartFixService _service;
    private readonly ILogger<IpcMessageHandler> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public IpcMessageHandler(ISmartFixService service, ILogger<IpcMessageHandler> logger)
    {
        _service = service;
        _logger  = logger;
    }

    public async Task<IpcResponse> HandleAsync(IpcRequest request, CancellationToken ct)
    {
        _logger.LogDebug("IPC {Command}", request.Command);
        try
        {
            return request.Command switch
            {
                IpcCommand.Ping                  => Pong(),
                IpcCommand.StartScan             => await StartScanAsync(request, ct),
                IpcCommand.GetScanStatus         => await GetScanStatusAsync(request, ct),
                IpcCommand.ExecuteRemediation    => await ExecuteRemediationAsync(request, ct),
                IpcCommand.BuildEscalationPacket => await BuildPacketAsync(request, ct),
                IpcCommand.ExportEscalationPacket => await ExportPacketAsync(request, ct),
                _ => Error($"Unknown command: {request.Command}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IPC handler error for {Command}", request.Command);
            return Error(ex.Message);
        }
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private static IpcResponse Pong() =>
        new() { Success = true, PayloadJson = "\"pong\"" };

    private async Task<IpcResponse> StartScanAsync(IpcRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Symptom))
            return Error("Symptom is required for StartScan");

        var scanId = await _service.StartScanAsync(req.Symptom, ct);
        // Return just the scanId string; UI polls GetScanStatus for progress
        return Ok(new { ScanId = scanId });
    }

    private async Task<IpcResponse> GetScanStatusAsync(IpcRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ScanId))
            return Error("ScanId is required for GetScanStatus");

        var dto = await _service.GetScanStatusAsync(req.ScanId, ct);
        return Ok(dto);
    }

    private async Task<IpcResponse> ExecuteRemediationAsync(
        IpcRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ScanId))
            return Error("ScanId is required");
        if (string.IsNullOrWhiteSpace(req.ActionInstanceId))
            return Error("ActionInstanceId is required");

        var action = await _service.ExecuteRemediationAsync(
            req.ScanId, req.ActionInstanceId, req.UserConsented, ct);
        return Ok(action);
    }

    private async Task<IpcResponse> BuildPacketAsync(IpcRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ScanId))
            return Error("ScanId is required");

        var packet = await _service.BuildEscalationPacketAsync(req.ScanId, req.Redact, ct);
        return Ok(packet);
    }

    private async Task<IpcResponse> ExportPacketAsync(IpcRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ScanId))
            return Error("ScanId is required");

        var packet = await _service.BuildEscalationPacketAsync(req.ScanId, req.Redact, ct);
        var dir = req.ExportDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LenovoSmartFix");
        var (json, pdf) = await _service.ExportEscalationPacketAsync(
            packet, dir, req.IncludePdf, ct);
        return Ok(new ExportResultDto { JsonPath = json, PdfPath = pdf });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IpcResponse Ok<T>(T payload) =>
        new() { Success = true, PayloadJson = JsonSerializer.Serialize(payload, JsonOpts) };

    private static IpcResponse Error(string message) =>
        new() { Success = false, Error = message };
}

using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LenovoSmartFix.Core.IPC;
using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.UI.Services;

/// <summary>
/// Sends IpcRequest messages to SmartFixService over the named pipe
/// and deserialises the IpcResponse payloads.
/// Uses the start/poll pattern: InitiateScanAsync returns a scanId immediately;
/// GetScanStatusAsync polls until the result is available.
/// </summary>
public sealed class SmartFixServiceProxy : ISmartFixServiceProxy
{
    private const string PipeName = "LenovoSmartFixPipe";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<string> InitiateScanAsync(
        string symptom, CancellationToken ct = default)
    {
        var response = await SendAsync(new IpcRequest
        {
            Command = IpcCommand.StartScan,
            Symptom = symptom
        }, ct);

        // Service returns { "ScanId": "..." } in the payload
        var payload = JsonSerializer.Deserialize<StartScanPayload>(
            response.PayloadJson!, JsonOpts)!;
        return payload.ScanId;
    }

    public async Task<ScanStatusDto> GetScanStatusAsync(
        string scanId, CancellationToken ct = default)
    {
        var response = await SendAsync(new IpcRequest
        {
            Command = IpcCommand.GetScanStatus,
            ScanId  = scanId
        }, ct);

        return Deserialize<ScanStatusDto>(response);
    }

    public async Task<RemediationAction> ExecuteRemediationAsync(
        string scanId, string actionInstanceId, bool userConsented,
        CancellationToken ct = default)
    {
        var response = await SendAsync(new IpcRequest
        {
            Command          = IpcCommand.ExecuteRemediation,
            ScanId           = scanId,
            ActionInstanceId = actionInstanceId,
            UserConsented    = userConsented
        }, ct);

        return Deserialize<RemediationAction>(response);
    }

    public async Task<EscalationPacket> BuildEscalationPacketAsync(
        string scanId, bool redact = true, CancellationToken ct = default)
    {
        var response = await SendAsync(new IpcRequest
        {
            Command = IpcCommand.BuildEscalationPacket,
            ScanId  = scanId,
            Redact  = redact
        }, ct);

        return Deserialize<EscalationPacket>(response);
    }

    public async Task<(string JsonPath, string? PdfPath)> ExportEscalationPacketAsync(
        string scanId, string exportDirectory,
        bool includePdf = true, bool redact = true,
        CancellationToken ct = default)
    {
        var response = await SendAsync(new IpcRequest
        {
            Command         = IpcCommand.ExportEscalationPacket,
            ScanId          = scanId,
            ExportDirectory = exportDirectory,
            IncludePdf      = includePdf,
            Redact          = redact
        }, ct);

        var result = Deserialize<ExportResultDto>(response);
        return (result.JsonPath, result.PdfPath);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private static async Task<IpcResponse> SendAsync(IpcRequest request, CancellationToken ct)
    {
        using var pipe = new NamedPipeClientStream(
            ".", PipeName, PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await pipe.ConnectAsync(10_000, ct);
        pipe.ReadMode = PipeTransmissionMode.Message;

        var requestJson = JsonSerializer.Serialize(request, JsonOpts);
        var bytes = Encoding.UTF8.GetBytes(requestJson);
        await pipe.WriteAsync(bytes, ct);
        await pipe.FlushAsync(ct);

        var buffer = new byte[524288]; // 512 KB max response
        using var ms = new MemoryStream();
        do
        {
            var read = await pipe.ReadAsync(buffer, ct);
            ms.Write(buffer, 0, read);
        } while (!pipe.IsMessageComplete);

        var response = JsonSerializer.Deserialize<IpcResponse>(ms.ToArray(), JsonOpts)
            ?? throw new InvalidOperationException("Null IPC response");

        if (!response.Success)
            throw new InvalidOperationException(response.Error ?? "Unknown service error");

        return response;
    }

    private static T Deserialize<T>(IpcResponse response)
    {
        if (string.IsNullOrEmpty(response.PayloadJson))
            throw new InvalidOperationException("Empty payload from service");
        return JsonSerializer.Deserialize<T>(response.PayloadJson, JsonOpts)
            ?? throw new InvalidOperationException("Null payload from service");
    }

    // ── Private DTOs ──────────────────────────────────────────────────────────

    private sealed class StartScanPayload
    {
        public string ScanId { get; set; } = string.Empty;
    }
}

using LenovoSmartFix.Core.IPC;
using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.UI.Services;

public interface ISmartFixServiceProxy
{
    /// <summary>Starts a scan in the background; returns the scanId immediately.</summary>
    Task<string> InitiateScanAsync(string symptom, CancellationToken ct = default);

    /// <summary>Returns live progress while running; full result once Completed/Failed.</summary>
    Task<ScanStatusDto> GetScanStatusAsync(string scanId, CancellationToken ct = default);

    Task<RemediationAction> ExecuteRemediationAsync(
        string scanId, string actionInstanceId, bool userConsented,
        CancellationToken ct = default);

    Task<EscalationPacket> BuildEscalationPacketAsync(
        string scanId, bool redact = true, CancellationToken ct = default);

    Task<(string JsonPath, string? PdfPath)> ExportEscalationPacketAsync(
        string scanId, string exportDirectory,
        bool includePdf = true, bool redact = true,
        CancellationToken ct = default);
}

using LenovoSmartFix.Core.IPC;
using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Core.Interfaces;

public interface ISmartFixService
{
    /// <summary>
    /// Creates a scan record and begins execution in the background.
    /// Returns immediately with the new scan ID; callers must poll
    /// <see cref="GetScanStatusAsync"/> for progress and the final result.
    /// </summary>
    Task<string> StartScanAsync(string symptom, CancellationToken ct = default);

    /// <summary>
    /// Returns current progress and, when complete, the full ScanResult.
    /// Safe to call repeatedly until Status == Completed or Failed.
    /// </summary>
    Task<ScanStatusDto> GetScanStatusAsync(string scanId, CancellationToken ct = default);

    /// <summary>
    /// Execute a single remediation action. Loads the scan and action from
    /// persisted storage — safe to call after a service restart.
    /// </summary>
    Task<RemediationAction> ExecuteRemediationAsync(
        string scanId,
        string actionInstanceId,
        bool userConsented,
        CancellationToken ct = default);

    /// <summary>
    /// Build the escalation packet for a completed scan. Loads all data
    /// from SQLite; does not require the scan to be in session memory.
    /// </summary>
    Task<EscalationPacket> BuildEscalationPacketAsync(
        string scanId,
        bool redact = true,
        CancellationToken ct = default);

    /// <summary>
    /// Export the escalation packet as JSON + optional PDF.
    /// Returns the file paths of the written artifacts.
    /// </summary>
    Task<(string JsonPath, string? PdfPath)> ExportEscalationPacketAsync(
        EscalationPacket packet,
        string exportDirectory,
        bool includePdf = true,
        CancellationToken ct = default);
}

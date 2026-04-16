using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Core.IPC;

// ── Commands ─────────────────────────────────────────────────────────────────

public enum IpcCommand
{
    Ping,
    StartScan,
    GetScanStatus,
    ExecuteRemediation,
    BuildEscalationPacket,
    ExportEscalationPacket
}

// ── Request ──────────────────────────────────────────────────────────────────

public sealed class IpcRequest
{
    public IpcCommand Command { get; set; }

    // StartScan
    public string? Symptom { get; set; }

    // GetScanStatus / ExecuteRemediation / BuildEscalationPacket / Export
    public string? ScanId { get; set; }

    // ExecuteRemediation
    public string? ActionInstanceId { get; set; }
    public bool UserConsented { get; set; }

    // BuildEscalationPacket / Export
    public bool Redact { get; set; } = true;

    // ExportEscalationPacket
    public string? ExportDirectory { get; set; }
    public bool IncludePdf { get; set; } = true;
}

// ── Response ─────────────────────────────────────────────────────────────────

public sealed class IpcResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    /// <summary>Serialised domain object payload (ScanStatusDto, RemediationAction, EscalationPacket, etc.)</summary>
    public string? PayloadJson { get; set; }
}

// ── Scan status DTO ──────────────────────────────────────────────────────────

/// <summary>
/// Lightweight status returned by GetScanStatus. Carries live progress while
/// running, and the full ScanResult (as JSON) once complete.
/// </summary>
public sealed class ScanStatusDto
{
    public string ScanId { get; set; } = string.Empty;
    public ScanStatus Status { get; set; }
    public int ProgressPercent { get; set; }
    public string ProgressStep { get; set; } = string.Empty;

    /// <summary>Non-null only when Status == Completed or Failed.</summary>
    public ScanResult? Result { get; set; }

    public string? ErrorMessage { get; set; }
}

// ── Export result DTO ────────────────────────────────────────────────────────

public sealed class ExportResultDto
{
    public string JsonPath { get; set; } = string.Empty;
    public string? PdfPath { get; set; }
}

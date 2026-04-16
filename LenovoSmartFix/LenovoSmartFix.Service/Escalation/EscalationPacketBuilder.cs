using System.Text.Json;
using System.Text.Json.Serialization;
using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Service.Escalation;

/// <summary>
/// Assembles and exports the EscalationPacket as JSON and PDF.
/// </summary>
public sealed class EscalationPacketBuilder
{
    private readonly ILogger<EscalationPacketBuilder> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public EscalationPacketBuilder(ILogger<EscalationPacketBuilder> logger) => _logger = logger;

    public EscalationPacket Build(
        string symptom,
        DeviceProfile device,
        HealthSnapshot health,
        UpdateStatus updates,
        DiagnosisDecision decision,
        IEnumerable<RemediationAction> actions,
        bool redact = true)
    {
        var attempted = actions.ToList();
        var resolved = attempted.Any(a => a.Result == RemediationResult.Success);

        return new EscalationPacket
        {
            PrimarySymptom = symptom,
            DeviceProfile = redact ? RedactDevice(device) : device,
            HealthSnapshot = health,
            UpdateStatus = updates,
            DiagnosisDecision = decision,
            ActionsAttempted = attempted,
            Outcome = resolved ? "Partially resolved" : "Unresolved — escalation recommended",
            UnresolvedReason = decision.UserFacingReason,
            IsRedacted = redact
        };
    }

    public async Task<string> ExportJsonAsync(
        EscalationPacket packet, string exportDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(exportDir);
        var fileName = $"SmartFix_Escalation_{packet.PacketId[..8]}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var path = Path.Combine(exportDir, fileName);
        var json = JsonSerializer.Serialize(packet, JsonOpts);
        await File.WriteAllTextAsync(path, json, ct);
        _logger.LogInformation("Escalation packet exported to {Path}", path);
        return path;
    }

    public async Task<string?> ExportPdfAsync(
        EscalationPacket packet, string exportDir, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(exportDir);
            var fileName = $"SmartFix_Summary_{packet.PacketId[..8]}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var path = Path.Combine(exportDir, fileName);

            // Use QuestPDF to generate a structured summary
            QuestPDF.Infrastructure.QuestPDF.Settings.License =
                QuestPDF.Infrastructure.LicenseType.Community;

            var document = new SmartFixPdfDocument(packet);
            document.GeneratePdf(path);

            _logger.LogInformation("PDF summary exported to {Path}", path);
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF generation failed; skipping PDF export");
            return null;
        }
    }

    private static DeviceProfile RedactDevice(DeviceProfile d) =>
        new()
        {
            DeviceId = "[redacted]",
            Model = d.Model,
            MachineType = d.MachineType,
            SerialNumber = "[redacted]",
            Manufacturer = d.Manufacturer,
            OsVersion = d.OsVersion,
            OsBuild = d.OsBuild,
            OsEdition = d.OsEdition,
            BiosVersion = d.BiosVersion,
            BiosDate = d.BiosDate,
            EcFirmwareVersion = d.EcFirmwareVersion,
            InstalledLenovoUtilities = d.InstalledLenovoUtilities,
            DriverInventory = d.DriverInventory,
            CollectedAt = d.CollectedAt
        };
}

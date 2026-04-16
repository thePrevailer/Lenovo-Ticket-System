using System.Text.Json.Serialization;

namespace LenovoSmartFix.Core.Models;

/// <summary>
/// Captures the full hardware and software identity of the device at scan time.
/// </summary>
public sealed class DeviceProfile
{
    public string DeviceId { get; init; } = string.Empty;

    // Hardware identity
    public string Model { get; init; } = string.Empty;
    public string MachineType { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;

    // OS
    public string OsVersion { get; init; } = string.Empty;
    public string OsBuild { get; init; } = string.Empty;
    public string OsEdition { get; init; } = string.Empty;

    // Firmware / BIOS
    public string BiosVersion { get; init; } = string.Empty;
    public string BiosDate { get; init; } = string.Empty;
    public string EcFirmwareVersion { get; init; } = string.Empty;

    // Installed Lenovo utilities (name -> version)
    public Dictionary<string, string> InstalledLenovoUtilities { get; init; } = new();

    // Driver inventory summary (device class -> driver version)
    public Dictionary<string, string> DriverInventory { get; init; } = new();

    // Support identifiers
    public string? WarrantyId { get; init; }
    public string? SupportContractId { get; init; }

    public DateTimeOffset CollectedAt { get; init; } = DateTimeOffset.UtcNow;
}

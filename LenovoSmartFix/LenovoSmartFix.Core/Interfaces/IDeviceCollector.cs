using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Core.Interfaces;

public interface IDeviceCollector
{
    Task<DeviceProfile> CollectAsync(CancellationToken ct = default);
}

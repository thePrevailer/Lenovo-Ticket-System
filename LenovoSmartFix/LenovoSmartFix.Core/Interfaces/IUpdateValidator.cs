using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Core.Interfaces;

public interface IUpdateValidator
{
    Task<UpdateStatus> ValidateAsync(DeviceProfile device, CancellationToken ct = default);
}

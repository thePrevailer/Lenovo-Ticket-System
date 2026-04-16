using LenovoSmartFix.Core.Models;

namespace LenovoSmartFix.Core.Interfaces;

public interface IHealthCollector
{
    Task<HealthSnapshot> CollectAsync(CancellationToken ct = default);
}

using CacheFramework.Models;
using CacheFramework.Models.Enums;

namespace CacheFramework.Interfaces;

public interface ISubscriptionRegistry
{
    void AddOrUpdate(SubscriptionEntry entry);

    bool Remove(string appId, ShapeType shape);

    SubscriptionEntry? Get(string appId, ShapeType shape);

    IReadOnlyList<SubscriptionEntry> GetByShape(ShapeType shape, FocusLevel? focusLevel = null);

    void UpdateFocus(string appId, ShapeType shape, FocusLevel focusLevel);

    void UpdateLastSync(string appId, ShapeType shape, DateTime timestamp);
}

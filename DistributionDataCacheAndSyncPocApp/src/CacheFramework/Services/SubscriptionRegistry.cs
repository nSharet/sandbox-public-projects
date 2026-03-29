using System.Collections.Concurrent;
using CacheFramework.Interfaces;
using CacheFramework.Models;
using CacheFramework.Models.Enums;

namespace CacheFramework.Services;

public class SubscriptionRegistry : ISubscriptionRegistry
{
    private readonly ConcurrentDictionary<string, SubscriptionEntry> _subscriptions = new();

    public void AddOrUpdate(SubscriptionEntry entry)
    {
        _subscriptions.AddOrUpdate(
            BuildKey(entry.AppId, entry.Shape),
            entry,
            (_, existing) =>
            {
                existing.FocusLevel = entry.FocusLevel;
                existing.Callback = entry.Callback;
                return existing;
            });
    }

    public bool Remove(string appId, ShapeType shape)
    {
        return _subscriptions.TryRemove(BuildKey(appId, shape), out _);
    }

    public SubscriptionEntry? Get(string appId, ShapeType shape)
    {
        return _subscriptions.TryGetValue(BuildKey(appId, shape), out var entry) ? entry : null;
    }

    public IReadOnlyList<SubscriptionEntry> GetByShape(ShapeType shape, FocusLevel? focusLevel = null)
    {
        return _subscriptions.Values
            .Where(s => s.Shape == shape && (focusLevel is null || s.FocusLevel == focusLevel))
            .ToList();
    }

    public void UpdateFocus(string appId, ShapeType shape, FocusLevel focusLevel)
    {
        var key = BuildKey(appId, shape);
        if (_subscriptions.TryGetValue(key, out var entry))
        {
            entry.FocusLevel = focusLevel;
        }
    }

    public void UpdateLastSync(string appId, ShapeType shape, DateTime timestamp)
    {
        var key = BuildKey(appId, shape);
        if (_subscriptions.TryGetValue(key, out var entry))
        {
            entry.LastSync = timestamp;
        }
    }

    private static string BuildKey(string appId, ShapeType shape) => $"{appId}::{shape}";
}

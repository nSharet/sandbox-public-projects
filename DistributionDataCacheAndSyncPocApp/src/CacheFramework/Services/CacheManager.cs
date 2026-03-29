using CacheFramework.Interfaces;
using CacheFramework.Models;
using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;
using Microsoft.Extensions.Logging;

namespace CacheFramework.Services;

public class CacheManager : ICacheManager
{
    private readonly ISubscriptionRegistry _registry;
    private readonly ICachePublisher _publisher;
    private readonly ITimestampManager _timestamps;
    private readonly ICacheStorage _storage;
    private readonly ILogger<CacheManager> _logger;

    public CacheManager(
        ISubscriptionRegistry registry,
        ICachePublisher publisher,
        ITimestampManager timestamps,
        ICacheStorage storage,
        ILogger<CacheManager> logger)
    {
        _registry = registry;
        _publisher = publisher;
        _timestamps = timestamps;
        _storage = storage;
        _logger = logger;
    }

    public void Subscribe(string appId, ShapeType shape, FocusLevel focusLevel, Action<NotificationData> callback)
    {
        _registry.AddOrUpdate(new SubscriptionEntry(appId, shape, focusLevel, callback));
        _logger.LogInformation("[{AppId}] Subscribed to {Shape} as {FocusLevel}", appId, shape, focusLevel);
    }

    public void Unsubscribe(string appId, ShapeType shape)
    {
        _registry.Remove(appId, shape);
        _logger.LogInformation("[{AppId}] Unsubscribed from {Shape}", appId, shape);
    }

    public SyncResult? ChangeFocus(string appId, ShapeType shape, FocusLevel focusLevel)
    {
        _registry.UpdateFocus(appId, shape, focusLevel);

        if (focusLevel != FocusLevel.InFocus)
        {
            return null;
        }

        var subscription = _registry.Get(appId, shape);
        var cacheTimestamp = _timestamps.Get(shape);

        if (subscription is null || cacheTimestamp is null)
        {
            return null;
        }

        if (cacheTimestamp.Value > subscription.LastSync)
        {
            var data = _storage.Get(shape);
            if (data is null)
            {
                return null;
            }

            _registry.UpdateLastSync(appId, shape, cacheTimestamp.Value);
            _logger.LogInformation("[{AppId}] Synced stale data for {Shape}", appId, shape);

            return new SyncResult
            {
                Shape = shape,
                Data = data,
                Timestamp = cacheTimestamp.Value
            };
        }

        return null;
    }

    public void Write(string appId, ShapeType shape, IShapeChange change)
    {
        if (change is not ShapeColorUpdated colorUpdated)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var data = new ShapeData
        {
            Shape = shape,
            Color = colorUpdated.NewColor,
            LastModified = now
        };

        _storage.Store(shape, data);
        _timestamps.Update(shape, now);
        _registry.UpdateLastSync(appId, shape, now);

        // Notify all subscribers (both InFocus and NotInFocus) except triggering app.
        // NotInFocus clients can keep a local cache warm and render instantly on focus switch.
        var subscribers = _registry.GetByShape(shape);
        var notification = new NotificationData(shape, change, appId, now);

        _publisher.NotifyAsync(subscribers, notification, appId);

        var notifiedCount = subscribers.Count(s => s.AppId != appId);
        _logger.LogInformation("[{AppId}] Changed {Shape}, notified {Count} subscribers", appId, shape, notifiedCount);
    }

    public ShapeData? Read(ShapeType shape)
    {
        return _storage.Get(shape);
    }

    public DateTime? GetLastModified(ShapeType shape)
    {
        return _timestamps.Get(shape);
    }
}

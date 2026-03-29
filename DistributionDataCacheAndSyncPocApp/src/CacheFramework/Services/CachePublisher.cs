using CacheFramework.Interfaces;
using CacheFramework.Models;
using Microsoft.Extensions.Logging;

namespace CacheFramework.Services;

public class CachePublisher : ICachePublisher
{
    private readonly ILogger<CachePublisher> _logger;

    public CachePublisher(ILogger<CachePublisher> logger)
    {
        _logger = logger;
    }

    public void NotifyAsync(IReadOnlyList<SubscriptionEntry> subscribers, NotificationData notification, string triggeringAppId)
    {
        foreach (var subscriber in subscribers)
        {
            if (subscriber.AppId == triggeringAppId)
            {
                continue;
            }

            Task.Run(() =>
            {
                try
                {
                    subscriber.Callback(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to notify {AppId} for {Shape}", subscriber.AppId, notification.Shape);
                }
            });
        }
    }
}

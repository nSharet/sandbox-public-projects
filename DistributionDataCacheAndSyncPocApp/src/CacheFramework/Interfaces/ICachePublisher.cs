using CacheFramework.Models;

namespace CacheFramework.Interfaces;

public interface ICachePublisher
{
    void NotifyAsync(IReadOnlyList<SubscriptionEntry> subscribers, NotificationData notification, string triggeringAppId);
}

using CacheFramework.Models.Enums;

namespace CacheFramework.Models;

/// <summary>
/// Represents a subscription by an application to a shape.
/// </summary>
public class SubscriptionEntry
{
    public SubscriptionEntry(
        string appId,
        ShapeType shape,
        FocusLevel focusLevel,
        Action<NotificationData> callback)
    {
        AppId = appId ?? throw new ArgumentNullException(nameof(appId));
        Shape = shape;
        FocusLevel = focusLevel;
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        SubscribedAt = DateTime.UtcNow;
        LastSync = DateTime.UtcNow;
    }

    public string AppId { get; }

    public ShapeType Shape { get; }

    public FocusLevel FocusLevel { get; set; }

    public Action<NotificationData> Callback { get; set; }

    public DateTime SubscribedAt { get; }

    public DateTime LastSync { get; set; }
}

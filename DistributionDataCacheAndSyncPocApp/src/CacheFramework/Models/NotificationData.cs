using CacheFramework.Interfaces;
using CacheFramework.Models.Enums;

namespace CacheFramework.Models;

/// <summary>
/// Data delivered to subscribers when a change occurs.
/// </summary>
public class NotificationData
{
    public NotificationData(ShapeType shape, IShapeChange change, string triggeredBy, DateTime timestamp)
    {
        Shape = shape;
        Change = change;
        TriggeredBy = triggeredBy;
        Timestamp = timestamp;
    }

    public ShapeType Shape { get; }

    public IShapeChange Change { get; }

    public string TriggeredBy { get; }

    public DateTime Timestamp { get; }
}

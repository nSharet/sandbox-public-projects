using CacheFramework.Interfaces;
using CacheFramework.Models;
using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;

namespace CacheFramework.Interfaces;

/// <summary>
/// Primary interface for applications to interact with the cache and synchronization flow.
/// </summary>
public interface ICacheManager
{
    void Subscribe(string appId, ShapeType shape, FocusLevel focusLevel, Action<NotificationData> callback);

    void Unsubscribe(string appId, ShapeType shape);

    SyncResult? ChangeFocus(string appId, ShapeType shape, FocusLevel focusLevel);

    void Write(string appId, ShapeType shape, IShapeChange change);

    ShapeData? Read(ShapeType shape);

    DateTime? GetLastModified(ShapeType shape);
}

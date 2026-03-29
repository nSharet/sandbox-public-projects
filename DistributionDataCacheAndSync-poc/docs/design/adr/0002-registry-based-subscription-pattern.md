# ADR-0002: Registry-Based Subscription Pattern

**Date:** 2026-03-23
**Status:** Accepted
**Deciders:** Architecture Team
**Context:** Notification delivery mechanism for cache updates

---

## Context

When the CommonVolumeCache Manager needs to notify subscribers about data changes, we need a mechanism to:
1. Track who is subscribed to which volumes/aspects
2. Deliver notifications to subscribers
3. Support async fire-and-forget delivery (per ADR-0001)

### Options Considered

**Option A: C# Events / Event Handlers**
```csharp
// Traditional .NET event pattern
public event EventHandler<CacheChangedEventArgs> CacheChanged;
```

**Option B: Registry-Based with Action Collection**
```csharp
// Registry stores app ID + callback action
Dictionary<SubscriptionKey, List<SubscriptionEntry>> subscriptions;

class SubscriptionEntry {
    string AppId;
    string VolumeId;
    Aspect? Aspect;       // null = volume-level (all aspects)
    FocusLevel Focus;
    Action<NotificationData> Callback;
}
```

---

## Decision

**We will use Option B: Registry-Based Subscription with Action Collection.**

The subscription registry maintains a collection of entries, each containing:
- `AppId` - Identifier of the subscribing application
- `VolumeId` - The volume being subscribed to
- `Aspect` - Optional enum specifying aspect (null = all aspects)
- `FocusLevel` - IN_FOCUS or NOT_IN_FOCUS
- `Callback` - An `Action<T>` delegate to invoke on notification

---

## Rationale

### Why NOT Event Handlers

| Issue | Impact |
|-------|--------|
| **No app identity** | Events don't carry subscriber identity; can't filter self-notifications |
| **Coupled invocation** | All handlers invoked synchronously by default |
| **Difficult cleanup** | Must track event subscriptions separately from business subscriptions |
| **No metadata** | Can't associate focus level, topic granularity with handler |
| **Multicast complexity** | Multicast delegates complicate individual error handling |

### Why Registry + Action Collection

| Benefit | How |
|---------|-----|
| **App identity** | SubscriptionEntry has AppId; easy to filter triggering app |
| **Rich metadata** | Store volumeId, aspect, focus level, timestamps alongside action |
| **Controlled invocation** | Framework decides when/how to call each action |
| **Async-friendly** | Easy to wrap each action in Task.Run() |
| **Independent failures** | One action failure doesn't affect others |
| **Query support** | Can query subscriptions by volume, aspect, app, focus level |

---

## Implementation

### Subscription Registry Structure

```csharp
public class SubscriptionRegistry
{
    // SubscriptionKey (VolumeId + Aspect?) -> List of subscribers
    private readonly ConcurrentDictionary<SubscriptionKey, List<SubscriptionEntry>> _byKey;
    
    // AppId -> List of subscriptions (for querying own subscriptions)
    private readonly ConcurrentDictionary<string, List<SubscriptionEntry>> _byApp;
    
    public void Subscribe(string appId, string volumeId, VolumeAspect? aspect, FocusLevel focus, Action<NotificationData> callback)
    {
        var key = new SubscriptionKey(volumeId, aspect);
        var entry = new SubscriptionEntry(appId, volumeId, aspect, focus, callback);
        _byKey.AddOrUpdate(key, ...);
        _byApp.AddOrUpdate(appId, ...);
    }
    
    public void Unsubscribe(string appId, string volumeId, VolumeAspect? aspect) { ... }
    
    public IEnumerable<SubscriptionEntry> GetSubscribers(string volumeId, VolumeAspect aspect, FocusLevel focus) { ... }
}
```

### SubscriptionKey

```csharp
public readonly struct SubscriptionKey : IEquatable<SubscriptionKey>
{
    public string VolumeId { get; }
    public VolumeAspect? Aspect { get; }    // null = volume-level subscription
    
    public SubscriptionKey(string volumeId, VolumeAspect? aspect = null)
    {
        VolumeId = volumeId;
        Aspect = aspect;
    }
}
```

### VolumeAspect Enum (Framework-Defined)

```csharp
/// <summary>
/// Aspects/sub-domains of a volume. Defined by the framework.
/// </summary>
public enum VolumeAspect
{
    Tissue,
    AnatomicalPath,
    TBD
}
```

### ChangeType Enum

```csharp
/// <summary>
/// Indicates what type of change occurred.
/// </summary>
public enum ChangeType
{
    Created,    // New item added
    Updated,    // Existing item modified
    Deleted     // Item removed
}
```

### SubscriptionEntry

```csharp
public class SubscriptionEntry
{
    public string AppId { get; }
    public string VolumeId { get; }
    public VolumeAspect? Aspect { get; }                  // null = all aspects of volume
    public FocusLevel FocusLevel { get; set; }      // IN_FOCUS or NOT_IN_FOCUS
    public Action<NotificationData> Callback { get; }
    public DateTime LastSync { get; set; }          // For NOT_IN_FOCUS timestamp tracking
}

public enum FocusLevel
{
    InFocus,
    NotInFocus
}
```

### Notification Delivery

```csharp
public class CommonVolumeCacheManager
{
    private readonly SubscriptionRegistry _registry;
    
    public void Write(string appId, string volumeId, VolumeAspect aspect, ChangeType changeType, object data)
    {
        // 1. Update CommonVolumeCache
        UpdateCache(volumeId, aspect, data);
        
        // 2. Get IN_FOCUS subscribers (hierarchical matching)
        var subscribers = GetMatchingSubscribers(volumeId, aspect, FocusLevel.InFocus);
        
        // 3. Filter out triggering app
        subscribers = subscribers.Where(s => s.AppId != appId);
        
        // 4. Async fire-and-forget to each (ADR-0001)
        foreach (var subscriber in subscribers)
        {
            _ = Task.Run(() => 
            {
                try
                {
                    subscriber.Callback(new NotificationData(volumeId, aspect, changeType, data, appId, DateTime.UtcNow));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification failed for {AppId} on {VolumeId}:{Aspect}", 
                        subscriber.AppId, volumeId, aspect);
                }
            });
        }
    }
    
    private IEnumerable<SubscriptionEntry> GetMatchingSubscribers(string volumeId, VolumeAspect aspect, FocusLevel focus)
    {
        var result = new List<SubscriptionEntry>();
        
        // 1. Aspect-level subscribers: (Volume1, Tissue)
        var aspectKey = new SubscriptionKey(volumeId, aspect);
        if (_registry.TryGetSubscribers(aspectKey, out var aspectSubs))
            result.AddRange(aspectSubs.Where(s => s.FocusLevel == focus));
        
        // 2. Volume-level subscribers: (Volume1, null) - want ALL aspects
        var volumeKey = new SubscriptionKey(volumeId, null);
        if (_registry.TryGetSubscribers(volumeKey, out var volumeSubs))
            result.AddRange(volumeSubs.Where(s => s.FocusLevel == focus));
        
        return result;
    }
}
```

---

## Comparison: Events vs Registry

| Aspect | Events | Registry + Actions |
|--------|--------|-------------------|
| Subscribe | `cache.Changed += handler` | `registry.Subscribe(appId, volumeId, aspect, focus, callback)` |
| Unsubscribe | `cache.Changed -= handler` | `registry.Unsubscribe(appId, volumeId, aspect)` |
| Filter by app | ❌ Not possible | ✅ `Where(s => s.AppId != triggeredBy)` |
| Filter by focus | ❌ Not possible | ✅ `Where(s => s.FocusLevel == InFocus)` |
| Hierarchical (volume/aspect) | ❌ Would need multiple events | ✅ Query both aspect key and volume key |
| Query own subscriptions | ❌ Track separately | ✅ `_byApp[appId]` |
| Async per-subscriber | ⚠️ Manual wrapping | ✅ Natural with Task.Run per entry |
| Type-safe aspects | ❌ String-based | ✅ VolumeAspect enum enforced at compile time |
| Change type (Create/Update/Delete) | ❌ Must infer | ✅ Explicit ChangeType in notification |

---

## Consequences

### Positive
- Full control over notification delivery
- Rich querying capability
- Natural fit for hierarchical pub/sub model
- Easy to implement ADR-0001 (async fire-and-forget)
- Supports all Epic 10 requirements (granular subscriptions)

### Negative
- More code than built-in events
- Must manage thread safety explicitly (ConcurrentDictionary)
- Must implement proper cleanup on app shutdown

### Risks
- Memory leak if subscriptions not cleaned up → Mitigate: Require explicit Unsubscribe, consider weak references

---

## Related Decisions

- **ADR-0001:** Async fire-and-forget execution model
- **Requirements:** Epic 4 (Subscription Management), Epic 10 (Hierarchical Subscription Model)

---

## References

- [Brainstorming: Two-Tier Subscription Model](../brainstorming.md)
- [Requirements v1.2](../requirements.md)

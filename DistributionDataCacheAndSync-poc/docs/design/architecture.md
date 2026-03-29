# Architecture: Distribution Data Cache and Sync Framework

**Date:** 2026-03-23
**Version:** 1.3
**Status:** Draft
**Phase:** Architecture

---

## Technology Stack

| Layer | Technology | Rationale |
|-------|------------|-----------|
| **Language** | C# / .NET | Existing codebase, team expertise |
| **Cache** | In-memory (`ConcurrentDictionary`) | Simple, fast, sufficient for single-process |
| **Subscriptions** | Registry pattern with Action delegates | No events; explicit app identity (ADR-0002) |
| **Threading** | `Task.Run()` for async notifications | Fire-and-forget per ADR-0001 |
| **Concurrency** | `ConcurrentDictionary`, `lock` where needed | Thread-safe collections |
| **Logging** | Microsoft.Extensions.Logging | Standard .NET logging |

### Why NOT Event Handlers

Per **ADR-0002**, we avoid C# events because:
- Events don't carry subscriber identity (can't filter self-notifications)
- Multicast delegates complicate per-subscriber error handling
- Can't associate rich metadata (topic, focus level) with event handlers

---

## System Context Diagram

```mermaid
C4Context
    title System Context: Distribution Data Cache and Sync

    Person(user, "End User", "Uses applications to view/edit data")
    
    System_Boundary(process, "Application Process") {
        System(app1, "Application 1", "e.g., Tissue Viewer")
        System(app2, "Application 2", "e.g., Anatomical Editor")  
        System(app3, "Application N", "Other applications")
        System(cache, "CommonVolumeCache Manager", "Manages shared data + subscriptions")
    }

    Rel(user, app1, "Views/Edits Volume:Tissue")
    Rel(user, app2, "Views/Edits Volume:AnatomicalPath")
    Rel(app1, cache, "Write + Subscribe")
    Rel(app2, cache, "Write + Subscribe")
    Rel(app3, cache, "Write + Subscribe")
    Rel(cache, app1, "Notify IN_FOCUS")
    Rel(cache, app2, "Notify IN_FOCUS")
```

---

## High-Level Conceptual Design

```mermaid
flowchart TB
    subgraph Applications["Applications Container"]
        direction LR
        App1["App 1<br/>(Tissue Viewer)"]
        App2["App 2<br/>(Anatomical Editor)"]
        AppN["App N<br/>(...)"]
    end

    subgraph CacheManager["CommonVolumeCache Manager"]
        direction TB
        API["Cache API"]
        Storage["Cache Storage<br/>+ Subscriptions"]
        Notifier["Notification<br/>Engine"]
        
        API --> Storage
        Storage --> Notifier
    end

    %% Write Flow
    App1 -->|"1. Write(change)"| API
    
    %% Subscribe Flow  
    App2 -.->|"Subscribe(topic, focus)"| API
    AppN -.->|"Subscribe(topic, focus)"| API
    
    %% Notify Flow
    Notifier ==>|"2. Notify(change)"| App2
    Notifier ==>|"2. Notify(change)"| AppN

    style Applications fill:#e1f5fe,stroke:#01579b
    style CacheManager fill:#fff3e0,stroke:#e65100
    style API fill:#ffcc80
    style Storage fill:#ffcc80
    style Notifier fill:#ffcc80
```

### Concept Summary

| Flow | Description |
|------|-------------|
| **Subscribe** | Applications register interest in specific topics with a focus level |
| **Write** | Writing app pushes changes to the cache; does NOT receive its own notification |
| **Notify** | All other subscribed apps (IN_FOCUS) receive async notifications |

---

## Component Diagram

```mermaid
C4Component
    title Component Diagram: CommonVolumeCache Manager

    Container_Boundary(ccm, "CommonVolumeCache Manager") {
        Component(api, "Cache API", "Public interface", "Subscribe, Unsubscribe, Write, Read, SwitchFocus")
        Component(registry, "Subscription Registry", "ConcurrentDictionary", "Tracks subscriptions by topic and app")
        Component(CommonVolumeCache, "CommonVolumeCache", "ConcurrentDictionary", "Stores shared data objects by topic")
        Component(notifier, "CommonVolumeCache Publisher", "Task.Run per subscriber", "Async fire-and-forget delivery")
        Component(timestamps, "Timestamp Manager", "Per-topic timestamps", "lastModified tracking")
    }
    
    Container_Boundary(app, "Application") {
        Component(privateCache, "VolumeCache", "App-specific objects", "Converted from common format")
        Component(converter, "Converter", "App implements", "Common ↔ App format")
        Component(callback, "CommonVolumeCache Callback", "Action<T>", "Processes incoming changes")
    }

    Rel(api, registry, "Register/query")
    Rel(api, CommonVolumeCache, "Read/Write")
    Rel(api, notifier, "Trigger notifications")
    Rel(api, timestamps, "Update/query")
    Rel(notifier, callback, "Invoke Action")
    Rel(callback, converter, "Convert to app format")
    Rel(converter, privateCache, "Store converted")
```

---

## Layered Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        APPLICATION LAYER                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │   App 1      │  │   App 2      │  │   App N      │           │
│  │ ┌──────────┐ │  │ ┌──────────┐ │  │ ┌──────────┐ │           │
│  │ │Private   │ │  │ │Private   │ │  │ │Private   │ │           │
│  │ │Cache     │ │  │ │Cache     │ │  │ │Cache     │ │           │
│  │ └──────────┘ │  │ └──────────┘ │  │ └──────────┘ │           │
│  │ ┌──────────┐ │  │ ┌──────────┐ │  │ ┌──────────┐ │           │
│  │ │Converter │ │  │ │Converter │ │  │ │Converter │ │           │
│  │ └──────────┘ │  │ └──────────┘ │  │ └──────────┘ │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└─────────────────────────────────────────────────────────────────┘
                              │
                    Subscribe │ Write │ Read
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     FRAMEWORK LAYER                              │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                   CommonVolumeCache Manager                     │  │
│  │  ┌─────────────────┐  ┌─────────────────┐                 │  │
│  │  │ Subscription    │  │ CommonVolumeCache    │                 │  │
│  │  │ Registry        │  │ (In-Memory)     │                 │  │
│  │  │                 │  │                 │                 │  │
│  │  │ Topic → [Entry] │  │ Topic → Data    │                 │  │
│  │  │ App → [Entry]   │  │                 │                 │  │
│  │  └─────────────────┘  └─────────────────┘                 │  │
│  │  ┌─────────────────┐  ┌─────────────────┐                 │  │
│  │  │ Notification    │  │ Timestamp       │                 │  │
│  │  │ Service         │  │ Manager         │                 │  │
│  │  │                 │  │                 │                 │  │
│  │  │ Task.Run()      │  │ Topic → DateTime│                 │  │
│  │  └─────────────────┘  └─────────────────┘                 │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Key Interfaces

### ICommonVolumeCacheManager

```csharp
public interface ICommonVolumeCacheManager
{
    // Subscription Management
    void Subscribe(string appId, string volumeId, VolumeAspect? aspect, FocusLevel focus, Action<NotificationData> callback);
    void Unsubscribe(string appId, string volumeId, VolumeAspect? aspect = null);
    void ChangeFocus(string appId, string volumeId, VolumeAspect? aspect, FocusLevel newFocus);
    IEnumerable<SubscriptionInfo> GetMySubscriptions(string appId);
    
    // Write Path - type-safe change object
    void Write(string appId, string volumeId, IVolumeAspectChange change);
    
    // Read Path (for focus switch)
    (object Data, DateTime Timestamp)? Read(string volumeId, VolumeAspect? aspect = null);
    DateTime? GetLastModified(string volumeId, VolumeAspect? aspect = null);
}
```

### ISubscriptionRegistry

```csharp
public interface ISubscriptionRegistry
{
    void Add(SubscriptionEntry entry);
    void Remove(string appId, string volumeId, VolumeAspect? aspect);
    void UpdateFocus(string appId, string volumeId, VolumeAspect? aspect, FocusLevel newFocus);
    
    IEnumerable<SubscriptionEntry> GetByVolume(string volumeId, VolumeAspect? aspect = null, FocusLevel? focus = null);
    IEnumerable<SubscriptionEntry> GetByApp(string appId);
    IEnumerable<SubscriptionEntry> GetMatchingSubscribers(string volumeId, VolumeAspect aspect, FocusLevel focus);
}
```

### ICommonVolumeCachePublisher

```csharp
public interface ICommonVolumeCachePublisher
{
    void NotifyAsync(IEnumerable<SubscriptionEntry> subscribers, NotificationData data, string excludeAppId);
}
```

---

## Data Structures

### VolumeAspect Enum (Framework-Defined)

```csharp
/// <summary>
/// Defines the aspects/sub-domains of a volume.
/// Defined by the CommonVolumeCache Manager (framework).
/// Applications subscribe to specific aspects or to all (null = volume-level).
/// </summary>
public enum VolumeAspect
{
    Tissue,
    AnatomicalPath,
    TBD                 // Placeholder for future aspects
}
```

### ChangeType Enum

```csharp
/// <summary>
/// Indicates what type of change occurred to the data.
/// </summary>
public enum ChangeType
{
    Created,    // New item added
    Updated,    // Existing item modified
    Deleted     // Item removed
}
```

### IVolumeAspectChange Interface (Type-Safe Changes)

```csharp
/// <summary>
/// Base interface for all volume aspect changes.
/// Enables type-safe change handling with C# pattern matching.
/// </summary>
public interface IVolumeAspectChange
{
    VolumeAspect Aspect { get; }
    ChangeType ChangeType { get; }
}
```

### Tissue Change Classes

```csharp
/// <summary>
/// Common data for all Tissue objects.
/// </summary>
public class TissueData
{
    public required string TissueId { get; init; }
    public required string Name { get; init; }
    public required string Color { get; init; }
    // ... other tissue properties
}

/// <summary>
/// Base class for Tissue changes.
/// </summary>
public abstract class TissueChange : IVolumeAspectChange
{
    public VolumeAspect Aspect => VolumeAspect.Tissue;
    public abstract ChangeType ChangeType { get; }
    
    /// <summary>
    /// The full tissue data - always included for all change types.
    /// </summary>
    public required TissueData Tissue { get; init; }
}

public class TissueCreated : TissueChange
{
    public override ChangeType ChangeType => ChangeType.Created;
    // Tissue property inherited - contains the new tissue data
}

public class TissueUpdated : TissueChange
{
    public override ChangeType ChangeType => ChangeType.Updated;
    // Tissue property inherited - contains the updated tissue data
}

public class TissueDeleted : TissueChange
{
    public override ChangeType ChangeType => ChangeType.Deleted;
    // Tissue property inherited - contains the deleted tissue data
}
```

### AnatomicalPath Change Classes

```csharp
public class AnatomicalPathData
{
    public required string PathId { get; init; }
    public required List<Point3D> Points { get; init; }
    // ... other path properties
}

public abstract class AnatomicalPathChange : IVolumeAspectChange
{
    public VolumeAspect Aspect => VolumeAspect.AnatomicalPath;
    public abstract ChangeType ChangeType { get; }
    
    /// <summary>
    /// The full path data - always included for all change types.
    /// </summary>
    public required AnatomicalPathData Path { get; init; }
}

public class AnatomicalPathCreated : AnatomicalPathChange
{
    public override ChangeType ChangeType => ChangeType.Created;
}

public class AnatomicalPathUpdated : AnatomicalPathChange
{
    public override ChangeType ChangeType => ChangeType.Updated;
}

public class AnatomicalPathDeleted : AnatomicalPathChange
{
    public override ChangeType ChangeType => ChangeType.Deleted;
}
```

### SubscriptionKey

```csharp
/// <summary>
/// Composite key for subscription lookups.
/// </summary>
public readonly struct SubscriptionKey : IEquatable<SubscriptionKey>
{
    public string VolumeId { get; }
    public VolumeAspect? Aspect { get; }    // null = volume-level subscription (all aspects)
    
    public SubscriptionKey(string volumeId, VolumeAspect? aspect = null)
    {
        VolumeId = volumeId;
        Aspect = aspect;
    }
    
    // Equality based on VolumeId + Aspect
    public bool Equals(SubscriptionKey other) => 
        VolumeId == other.VolumeId && Aspect == other.Aspect;
        
    public override int GetHashCode() => HashCode.Combine(VolumeId, Aspect);
}
```

### SubscriptionEntry

```csharp
public class SubscriptionEntry
{
    public string AppId { get; }
    public string VolumeId { get; }
    public VolumeAspect? Aspect { get; }              // null = subscribe to ALL aspects of volume
    public FocusLevel FocusLevel { get; set; }
    public Action<NotificationData> Callback { get; }
    public DateTime SubscribedAt { get; }
    public DateTime LastSync { get; set; }      // For NOT_IN_FOCUS timestamp tracking
    
    public SubscriptionKey Key => new(VolumeId, Aspect);
}
```

### NotificationData

```csharp
public class NotificationData
{
    public string VolumeId { get; }             // The volume that changed
    public IVolumeAspectChange Change { get; }  // Type-safe change object with full data
    public string TriggeredBy { get; }          // App ID that made the change
    public DateTime Timestamp { get; }
    
    // Convenience properties derived from Change
    public VolumeAspect Aspect => Change.Aspect;
    public ChangeType ChangeType => Change.ChangeType;
}
```

### Subscriber Pattern Matching Example

```csharp
void OnNotification(NotificationData notification)
{
    switch (notification.Change)
    {
        case TissueCreated tc:
            // tc.Tissue contains the full new tissue data
            AddToPrivateCache(tc.Tissue);
            break;
            
        case TissueUpdated tu:
            // tu.Tissue contains the full updated tissue data
            UpdateInPrivateCache(tu.Tissue);
            break;
            
        case TissueDeleted td:
            // td.Tissue contains the full deleted tissue data
            RemoveFromPrivateCache(td.Tissue.TissueId);
            LogDeletedTissue(td.Tissue);  // Can still access all properties!
            break;
            
        case AnatomicalPathCreated ac:
            AddPath(ac.Path);
            break;
            
        case AnatomicalPathUpdated au:
            UpdatePath(au.Path);
            break;
            
        case AnatomicalPathDeleted ad:
            RemovePath(ad.Path.PathId);
            break;
    }
}
```

### FocusLevel

```csharp
public enum FocusLevel
{
    InFocus,        // Receives real-time push notifications
    NotInFocus      // No push; pull on focus switch
}
```

---

## Data Flow Diagrams

### Write Path (with IN_FOCUS notification)

```mermaid
sequenceDiagram
    participant App1 as App 1 (Writer)
    participant CCM as CommonVolumeCacheManager
    participant Cache as CommonVolumeCache
    participant Reg as Subscription Registry
    participant NS as CommonVolumeCache Publisher
    participant App2 as App 2 (IN_FOCUS)
    participant App3 as App 3 (NOT_IN_FOCUS)

    App1->>CCM: Write("App1", "Vol1", new TissueCreated { Tissue = data })
    CCM->>Cache: Store data at (Vol1, Tissue)
    CCM->>Cache: Update timestamp[Vol1, Tissue]
    CCM->>Cache: Update timestamp[Vol1, null]
    
    CCM->>Reg: GetMatchingSubscribers("Vol1", VolumeAspect.Tissue, IN_FOCUS)
    Reg-->>CCM: [App2(Vol1, null), App3(Vol1, Tissue)]
    
    CCM->>CCM: Filter out App1 (triggeredBy)
    CCM->>CCM: Filter by IN_FOCUS only
    
    CCM->>NS: NotifyAsync([App2], notification, excludeAppId: "App1")
    NS->>NS: Task.Run() per subscriber
    NS-->>App2: Callback({Vol1, TissueCreated { Tissue }})
    
    Note over App2: Pattern match TissueCreated -> Add to VolumeCache
    Note over App3: NOT_IN_FOCUS - no notification
```

### Focus Switch Path (Pull)

```mermaid
sequenceDiagram
    participant App2 as App 2
    participant CCM as CommonVolumeCacheManager
    participant Reg as Subscription Registry
    participant Cache as CommonVolumeCache

    App2->>CCM: ChangeFocus("App2", "Vol1", VolumeAspect.Tissue, IN_FOCUS)
    CCM->>Reg: UpdateFocus("App2", "Vol1", VolumeAspect.Tissue, IN_FOCUS)
    
    CCM->>Cache: GetLastModified("Vol1", VolumeAspect.Tissue)
    Cache-->>CCM: lastModified = T2
    
    CCM->>CCM: Compare T2 > App2.LastSync?
    
    alt Data is stale
        CCM->>Cache: Read("Vol1", VolumeAspect.Tissue)
        Cache-->>CCM: data
        CCM-->>App2: (data, T2)
        App2->>App2: Convert to app format
        App2->>App2: Update VolumeCache
        App2->>App2: Update LastSync = T2
    else Data is current
        CCM-->>App2: null (no sync needed)
    end
```

---

## Hierarchical Subscription Matching

When a write occurs to `(Volume1, VolumeAspect.Tissue)`:

```
Write("Volume1", new TissueUpdated { Tissue = updatedData })
    │
    ├── Notify subscribers[(Volume1, Tissue)]    (aspect subscribers)
    │
    └── Notify subscribers[(Volume1, null)]      (volume subscribers - ALL aspects)
    
Subscribers to (Volume1, AnatomicalPath) are NOT notified.
```

### Implementation

```csharp
public IEnumerable<SubscriptionEntry> GetMatchingSubscribers(string volumeId, VolumeAspect aspect, FocusLevel focus)
{
    var result = new List<SubscriptionEntry>();
    
    // 1. Direct aspect match: subscribers to (volumeId, aspect)
    var aspectKey = new SubscriptionKey(volumeId, aspect);
    if (_byKey.TryGetValue(aspectKey, out var aspectSubscribers))
        result.AddRange(aspectSubscribers.Where(s => s.FocusLevel == focus));
    
    // 2. Volume-level match: subscribers to (volumeId, null) - they want ALL aspects
    var volumeKey = new SubscriptionKey(volumeId, null);
    if (_byKey.TryGetValue(volumeKey, out var volumeSubscribers))
        result.AddRange(volumeSubscribers.Where(s => s.FocusLevel == focus));
    
    return result;
}
```

### Subscription Examples

```csharp
// Subscribe to ALL aspects of Volume1 (volume-level)
cacheManager.Subscribe("App1", "Volume1", aspect: null, FocusLevel.InFocus, callback);
// -> Receives notifications for ANY change to Volume1

// Subscribe to ONLY Tissue aspect of Volume1
cacheManager.Subscribe("App2", "Volume1", VolumeAspect.Tissue, FocusLevel.InFocus, callback);
// -> Receives notifications ONLY when Tissue changes

// Create new Tissue - full data included
var newTissue = new TissueData { TissueId = "T1", Name = "Liver", Color = "#8B0000" };
cacheManager.Write("App3", "Volume1", new TissueCreated { Tissue = newTissue });
// -> App1 & App2 receive TissueCreated with full Tissue data
// -> Subscriber pattern matches: case TissueCreated tc => AddToCache(tc.Tissue)

// Update Tissue (e.g., color change) - full data included
var updatedTissue = new TissueData { TissueId = "T1", Name = "Liver", Color = "#FF0000" };
cacheManager.Write("App3", "Volume1", new TissueUpdated { Tissue = updatedTissue });
// -> Subscribers receive TissueUpdated with full updated Tissue data
// -> Subscriber pattern matches: case TissueUpdated tu => UpdateInCache(tu.Tissue)

// Delete Tissue - full data still included!
var deletedTissue = new TissueData { TissueId = "T1", Name = "Liver", Color = "#FF0000" };
cacheManager.Write("App3", "Volume1", new TissueDeleted { Tissue = deletedTissue });
// -> Subscribers receive TissueDeleted with full Tissue data
// -> Subscriber can: RemoveFromCache(td.Tissue.TissueId) AND log td.Tissue.Name
```

---

## Thread Safety

| Component | Strategy |
|-----------|----------|
| Subscription Registry | `ConcurrentDictionary` + `lock` on list modifications |
| CommonVolumeCache | `ConcurrentDictionary` |
| Timestamp Manager | `ConcurrentDictionary` |
| Notification delivery | Each callback in separate `Task.Run()` |

---

## Architectural Decisions

| ADR | Decision | Reference |
|-----|----------|-----------|
| ADR-0001 | Async fire-and-forget notifications | [Link](adr/0001-async-notification-execution-model.md) |
| ADR-0002 | Registry-based subscriptions (no events) | [Link](adr/0002-registry-based-subscription-pattern.md) |

---

## Dependencies

```
Framework Components (this POC):
├── CommonVolumeCacheManager
├── SubscriptionRegistry  
├── CommonVolumeCachePublisher
└── TimestampManager

Application Responsibility:
├── VolumeCache (app manages internally)
├── Converter (app implements IConverter)
└── CommonVolumeCache Callback (app provides Action<T>)
```

---

## Sign-off

| Role | Name | Date | Approved |
|------|------|------|----------|
| Tech Lead | | | [ ] |
| Principal Engineer | | | [ ] |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-03-23 | | Initial architecture based on requirements v1.2 |
| 1.1 | 2026-03-23 | | Replaced string topic with VolumeId + Aspect enum. Added SubscriptionKey struct. |
| 1.2 | 2026-03-23 | | Renamed Aspect to VolumeAspect. Added ChangeType enum (Created/Updated/Deleted). |
| 1.3 | 2026-03-23 | | Added IVolumeAspectChange interface with type-safe change classes. All change types include full data. |
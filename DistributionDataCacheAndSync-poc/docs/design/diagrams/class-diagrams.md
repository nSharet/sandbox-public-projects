# Class Diagrams: Distribution Data Cache and Sync Framework

**Date:** 2026-03-23
**Version:** 1.2
**Architecture Reference:** [architecture.md](../architecture.md)
**Requirements Reference:** [requirements.md](../requirements.md)

---

## Overview

This document contains detailed class diagrams for all framework components.

---

## Complete Class Diagram

```mermaid
classDiagram
    direction TB
    
    %% === INTERFACES ===
    class ICommonVolumeCacheManager {
        <<interface>>
        +Subscribe(appId, volumeId, aspect, focus, callback) void
        +Unsubscribe(appId, volumeId, aspect) void
        +UnsubscribeAll(appId) int
        +ChangeFocus(appId, volumeId, aspect, newFocus) SyncResult?
        +ChangeFocusAll(appId, newFocus) IEnumerable~SyncResult~
        +GetMySubscriptions(appId) IEnumerable~SubscriptionInfo~
        +Write(appId, volumeId, change) void
        +Read(volumeId, aspect) Tuple~object, DateTime~?
        +GetLastModified(volumeId, aspect) DateTime?
    }
    
    class ISubscriptionRegistry {
        <<interface>>
        +Add(entry) void
        +Remove(appId, volumeId, aspect) void
        +RemoveAll(appId) int
        +UpdateFocus(appId, volumeId, aspect, newFocus) void
        +UpdateFocusAll(appId, newFocus) void
        +GetByVolume(volumeId, aspect, focus) IEnumerable~SubscriptionEntry~
        +GetByApp(appId) IEnumerable~SubscriptionEntry~
        +GetMatchingSubscribers(volumeId, aspect, focus) IEnumerable~SubscriptionEntry~
    }
    
    class ICommonVolumeCachePublisher {
        <<interface>>
        +NotifyAsync(subscribers, data, excludeAppId) void
    }
    
    class ITimestampManager {
        <<interface>>
        +Update(volumeId, aspect) void
        +Get(volumeId, aspect) DateTime?
        +GetVolume(volumeId) DateTime?
    }
    
    class ICommonVolumeCache {
        <<interface>>
        +Store(volumeId, aspect, data) void
        +Get(volumeId, aspect) object?
        +Remove(volumeId, aspect) bool
        +GetAll(volumeId) Dictionary~VolumeAspect, object~
    }
    
    class IVolumeAspectChange {
        <<interface>>
        +Aspect VolumeAspect
        +ChangeType ChangeType
    }
    
    class IGenerator {
        <<interface>>
        +GeneratorId string
        +TargetApplications IEnumerable~string~
        +StartAsync(cacheManager, ct) Task
        +StopAsync(ct) Task
        +PreRegisterApplication(appId, cacheManager, callback) void
        +PreLoadDataAsync(appId, privateCache) Task
    }
    
    class IPrivateCache {
        <<interface>>
        +Store(volumeId, aspect, data) void
        +Get(volumeId, aspect) object?
        +GetLastSync(volumeId, aspect) DateTime
        +UpdateLastSync(volumeId, aspect, timestamp) void
    }
    
    %% === IMPLEMENTATIONS ===
    class CommonVolumeCacheManager {
        -ISubscriptionRegistry _registry
        -ICommonVolumeCachePublisher _notifier
        -ITimestampManager _timestamps
        -ICommonVolumeCache _cache
        -ILogger _logger
        +Subscribe(appId, volumeId, aspect, focus, callback) void
        +Unsubscribe(appId, volumeId, aspect) void
        +UnsubscribeAll(appId) int
        +ChangeFocus(appId, volumeId, aspect, newFocus) SyncResult?
        +ChangeFocusAll(appId, newFocus) IEnumerable~SyncResult~
        +GetMySubscriptions(appId) IEnumerable~SubscriptionInfo~
        +Write(appId, volumeId, change) void
        +Read(volumeId, aspect) Tuple~object, DateTime~?
        +GetLastModified(volumeId, aspect) DateTime?
    }
    
    class SubscriptionRegistry {
        -ConcurrentDictionary~SubscriptionKey, List~SubscriptionEntry~~ _byKey
        -ConcurrentDictionary~string, List~SubscriptionEntry~~ _byApp
        -object _lock
        +Add(entry) void
        +Remove(appId, volumeId, aspect) void
        +RemoveAll(appId) int
        +UpdateFocus(appId, volumeId, aspect, newFocus) void
        +UpdateFocusAll(appId, newFocus) void
        +GetByVolume(volumeId, aspect, focus) IEnumerable~SubscriptionEntry~
        +GetByApp(appId) IEnumerable~SubscriptionEntry~
        +GetMatchingSubscribers(volumeId, aspect, focus) IEnumerable~SubscriptionEntry~
    }
    
    class CommonVolumeCachePublisher {
        -ILogger _logger
        +NotifyAsync(subscribers, data, excludeAppId) void
        -DeliverToSubscriber(entry, data) Task
    }
    
    class TimestampManager {
        -ConcurrentDictionary~SubscriptionKey, DateTime~ _timestamps
        +Update(volumeId, aspect) void
        +Get(volumeId, aspect) DateTime?
        +GetVolume(volumeId) DateTime?
    }
    
    class CommonVolumeCache {
        -ConcurrentDictionary~SubscriptionKey, object~ _data
        +Store(volumeId, aspect, data) void
        +Get(volumeId, aspect) object?
        +Remove(volumeId, aspect) bool
        +GetAll(volumeId) Dictionary~VolumeAspect, object~
    }
    
    %% === RELATIONSHIPS ===
    ICommonVolumeCacheManager <|.. CommonVolumeCacheManager
    ISubscriptionRegistry <|.. SubscriptionRegistry
    ICommonVolumeCachePublisher <|.. CommonVolumeCachePublisher
    ITimestampManager <|.. TimestampManager
    ICommonVolumeCache <|.. CommonVolumeCache
    
    CommonVolumeCacheManager --> ISubscriptionRegistry : uses
    CommonVolumeCacheManager --> ICommonVolumeCachePublisher : uses
    CommonVolumeCacheManager --> ITimestampManager : uses
    CommonVolumeCacheManager --> ICommonVolumeCache : uses
    
    IGenerator --> ICommonVolumeCacheManager : uses
    IGenerator --> IPrivateCache : pre-loads
```

---

## Data Structures Class Diagram

```mermaid
classDiagram
    direction TB
    
    %% === ENUMS ===
    class VolumeAspect {
        <<enumeration>>
        Tissue
        AnatomicalPath
        TBD
    }
    
    class ChangeType {
        <<enumeration>>
        Created
        Updated
        Deleted
    }
    
    class FocusLevel {
        <<enumeration>>
        InFocus
        NotInFocus
    }
    
    %% === VALUE TYPES ===
    class SubscriptionKey {
        <<struct>>
        +string VolumeId
        +VolumeAspect? Aspect
        +SubscriptionKey(volumeId, aspect)
        +Equals(other) bool
        +GetHashCode() int
    }
    
    %% === ENTITIES ===
    class SubscriptionEntry {
        +string AppId
        +string VolumeId
        +VolumeAspect? Aspect
        +FocusLevel FocusLevel
        +Action~NotificationData~ Callback
        +DateTime SubscribedAt
        +DateTime LastSync
        +Key SubscriptionKey
    }
    
    class SubscriptionInfo {
        +string VolumeId
        +VolumeAspect? Aspect
        +FocusLevel FocusLevel
        +DateTime SubscribedAt
        +DateTime LastSync
    }
    
    class NotificationData {
        +string VolumeId
        +IVolumeAspectChange Change
        +string TriggeredBy
        +DateTime Timestamp
        +Aspect VolumeAspect
        +ChangeType ChangeType
    }
    
    %% === RELATIONSHIPS ===
    SubscriptionEntry --> SubscriptionKey : has
    SubscriptionEntry --> FocusLevel : has
    SubscriptionEntry --> VolumeAspect : optional
    SubscriptionEntry --> NotificationData : receives
    NotificationData --> IVolumeAspectChange : contains
    NotificationData --> VolumeAspect : derived
    NotificationData --> ChangeType : derived
    SubscriptionInfo --> VolumeAspect : optional
    SubscriptionInfo --> FocusLevel : has
```

---

## Change Classes Hierarchy

```mermaid
classDiagram
    direction TB
    
    %% === BASE INTERFACE ===
    class IVolumeAspectChange {
        <<interface>>
        +Aspect VolumeAspect
        +ChangeType ChangeType
    }
    
    %% === TISSUE CHANGES ===
    class TissueData {
        +string TissueId
        +string Name
        +string Color
    }
    
    class TissueChange {
        <<abstract>>
        +Aspect VolumeAspect = VolumeAspect.Tissue
        +ChangeType ChangeType*
        +TissueData Tissue
    }
    
    class TissueCreated {
        +ChangeType ChangeType = Created
    }
    
    class TissueUpdated {
        +ChangeType ChangeType = Updated
    }
    
    class TissueDeleted {
        +ChangeType ChangeType = Deleted
    }
    
    %% === ANATOMICAL PATH CHANGES ===
    class AnatomicalPathData {
        +string PathId
        +List~Point3D~ Points
    }
    
    class AnatomicalPathChange {
        <<abstract>>
        +Aspect VolumeAspect = VolumeAspect.AnatomicalPath
        +ChangeType ChangeType*
        +AnatomicalPathData Path
    }
    
    class AnatomicalPathCreated {
        +ChangeType ChangeType = Created
    }
    
    class AnatomicalPathUpdated {
        +ChangeType ChangeType = Updated
    }
    
    class AnatomicalPathDeleted {
        +ChangeType ChangeType = Deleted
    }
    
    %% === RELATIONSHIPS ===
    IVolumeAspectChange <|.. TissueChange
    TissueChange <|-- TissueCreated
    TissueChange <|-- TissueUpdated
    TissueChange <|-- TissueDeleted
    TissueChange --> TissueData : contains
    
    IVolumeAspectChange <|.. AnatomicalPathChange
    AnatomicalPathChange <|-- AnatomicalPathCreated
    AnatomicalPathChange <|-- AnatomicalPathUpdated
    AnatomicalPathChange <|-- AnatomicalPathDeleted
    AnatomicalPathChange --> AnatomicalPathData : contains
```

---

## Application-Side Architecture

```mermaid
classDiagram
    direction TB
    
    %% === APPLICATION COMPONENTS ===
    class Application {
        -string _appId
        -ICommonVolumeCacheManager _cacheManager
        -IPrivateCache _privateCache
        -IConverter _converter
        +Initialize() void
        +SubscribeToVolume(volumeId, focus) void
        +OnNotification(data) void
        +SwitchFocus(volumeId, aspect) void
    }
    
    class IPrivateCache {
        <<interface>>
        +Store(key, appSpecificObject) void
        +Get(key) TAppObject?
        +Remove(key) bool
        +GetLastSync(volumeId, aspect) DateTime
        +UpdateLastSync(volumeId, aspect, timestamp) void
    }
    
    class IConverter {
        <<interface>>
        +ToCommon(appObject) IVolumeAspectChange
        +FromCommon(change) TAppObject
    }
    
    class PrivateCache~TAppObject~ {
        -Dictionary~string, TAppObject~ _data
        -Dictionary~SubscriptionKey, DateTime~ _syncTimestamps
        +Store(key, appSpecificObject) void
        +Get(key) TAppObject?
        +Remove(key) bool
        +GetLastSync(volumeId, aspect) DateTime
        +UpdateLastSync(volumeId, aspect, timestamp) void
    }
    
    %% === RELATIONSHIPS ===
    Application --> ICommonVolumeCacheManager : uses
    Application --> IPrivateCache : uses
    Application --> IConverter : uses
    IPrivateCache <|.. PrivateCache
```

---

## Dependency Injection Structure

```mermaid
classDiagram
    direction TB
    
    class IServiceCollection {
        <<interface>>
        +AddSingleton~T~() void
        +AddScoped~T~() void
    }
    
    class CacheManagerServiceExtensions {
        <<static>>
        +AddCommonVolumeCacheManager(services) IServiceCollection
    }
    
    class ServiceProvider {
        +GetService~T~() T
        +GetRequiredService~T~() T
    }
    
    note for CacheManagerServiceExtensions "Registers:\n- ICommonVolumeCacheManager → CommonVolumeCacheManager (Singleton)\n- ISubscriptionRegistry → SubscriptionRegistry (Singleton)\n- ICommonVolumeCachePublisher → CommonVolumeCachePublisher (Singleton)\n- ITimestampManager → TimestampManager (Singleton)\n- ICommonVolumeCache → CommonVolumeCache (Singleton)"
```

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-03-23 | | Initial class diagrams |
| 1.1 | 2026-03-23 | | Added UnsubscribeAll, RemoveAll, ChangeFocusAll, UpdateFocusAll methods || 1.2 | 2026-03-23 | | Added IGenerator, IPrivateCache interfaces |
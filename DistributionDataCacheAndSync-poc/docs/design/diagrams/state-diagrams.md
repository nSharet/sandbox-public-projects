# State Diagrams: Distribution Data Cache and Sync Framework

**Date:** 2026-03-23
**Version:** 1.0
**Architecture Reference:** [architecture.md](../architecture.md)
**Requirements Reference:** [requirements.md](../requirements.md)

---

## Overview

This document contains state diagrams for the key entities and lifecycles in the framework.

---

## 1. Subscription Lifecycle

**Requirements:** US-006 to US-009

```mermaid
stateDiagram-v2
    direction LR
    
    [*] --> Unsubscribed : Initial
    
    Unsubscribed --> InFocus : Subscribe(IN_FOCUS)
    Unsubscribed --> NotInFocus : Subscribe(NOT_IN_FOCUS)
    
    InFocus --> NotInFocus : ChangeFocus(NOT_IN_FOCUS)
    NotInFocus --> InFocus : ChangeFocus(IN_FOCUS)\n[triggers sync check]
    
    InFocus --> Unsubscribed : Unsubscribe()
    NotInFocus --> Unsubscribed : Unsubscribe()
    
    state InFocus {
        [*] --> Listening
        Listening --> Processing : notification received
        Processing --> Listening : callback complete
        Processing --> Listening : callback failed\n[logged, continues]
    }
    
    state NotInFocus {
        [*] --> Tracking
        Tracking --> Stale : CommonVolumeCache updated
        Stale --> Tracking : focus switch\n[sync performed]
    }
```

### State Descriptions

| State | Description | Behavior |
|-------|-------------|----------|
| **Unsubscribed** | App has no subscription for this topic | No notifications, no tracking |
| **InFocus** | Actively listening for real-time updates | Push notifications delivered async |
| **NotInFocus** | Tracking but not actively presenting | No push; pull on focus switch |
| **Listening** | Ready to receive notifications | Waiting for callback invocation |
| **Processing** | Callback executing | May convert data, update VolumeCache |
| **Tracking** | Monitoring for staleness | Compares timestamps on focus switch |
| **Stale** | CommonVolumeCache newer than last sync | Needs sync before display |

---

## 2. Notification Delivery Lifecycle

**Requirements:** US-010, US-011, US-017

```mermaid
stateDiagram-v2
    direction TB
    
    [*] --> Created : Write() called
    
    Created --> Queued : Subscribers identified
    
    Queued --> Delivering : Task.Run() started
    
    state Delivering {
        [*] --> InvokeCallback
        InvokeCallback --> Success : callback returned
        InvokeCallback --> Failed : exception thrown
    }
    
    Delivering --> Completed : all tasks finished
    
    Completed --> [*]
    
    note right of Failed
        Error is logged
        Does NOT retry
        Does NOT rollback
        Other deliveries continue
    end note
    
    note right of Created
        notification = {
            VolumeId,
            Change,
            TriggeredBy,
            Timestamp
        }
    end note
```

### Notification States

| State | Description | Next Actions |
|-------|-------------|--------------|
| **Created** | NotificationData object constructed | Query registry for subscribers |
| **Queued** | Subscribers identified, ready to dispatch | Start async tasks |
| **Delivering** | Task.Run() executing callback | Wait for completion |
| **Success** | Callback completed without exception | Log success |
| **Failed** | Callback threw exception | Log error, continue |
| **Completed** | All delivery tasks finished | Cleanup |

---

## 3. Cache Entry Lifecycle

**Requirements:** US-002, US-005

```mermaid
stateDiagram-v2
    direction TB
    
    [*] --> Empty : Initial
    
    Empty --> Present : Write(Created)
    
    Present --> Present : Write(Updated)\n[data replaced]
    Present --> Deleted : Write(Deleted)
    
    Deleted --> Present : Write(Created)\n[same key]
    Deleted --> [*] : Cleanup (optional)
    
    state Present {
        [*] --> Valid
        Valid --> Valid : timestamp updated
    }
```

### Cache States

| State | Description | Timestamp |
|-------|-------------|-----------|
| **Empty** | No data for this (VolumeId, Aspect) key | N/A |
| **Present** | Data exists in cache | Updated on every write |
| **Valid** | Data is complete and accessible | Current |
| **Deleted** | Tombstone or removed | Final timestamp before delete |

---

## 4. Focus Level State Machine

**Requirements:** US-009

```mermaid
stateDiagram-v2
    direction LR
    
    [*] --> NotInFocus : Default subscription
    [*] --> InFocus : Explicit focus
    
    InFocus --> NotInFocus : User switches away
    NotInFocus --> InFocus : User switches to\n[sync if stale]
    
    InFocus --> [*] : Unsubscribe
    NotInFocus --> [*] : Unsubscribe
    
    note right of InFocus
        Receives push notifications
        Real-time updates
    end note
    
    note right of NotInFocus
        No push notifications
        Tracks staleness
        Pull on focus switch
    end note
```

---

## 5. Application Registration Lifecycle

```mermaid
stateDiagram-v2
    direction TB
    
    [*] --> Unregistered : App starting
    
    Unregistered --> Active : First Subscribe()
    
    state Active {
        [*] --> HasSubscriptions
        HasSubscriptions --> HasSubscriptions : Subscribe/Unsubscribe
        HasSubscriptions --> Empty : Last Unsubscribe()
        Empty --> HasSubscriptions : Subscribe()
    }
    
    Active --> Unregistered : All subscriptions removed\nOR App shutdown
    
    Unregistered --> [*] : App terminated
```

---

## 6. Data Sync State (Per Subscription)

**Requirements:** US-013, US-014

```mermaid
stateDiagram-v2
    direction LR
    
    [*] --> Current : Initial sync
    
    Current --> Stale : commonTimestamp > lastSync
    Stale --> Current : ChangeFocus(IN_FOCUS)\n[sync performed]
    
    Current --> Current : Notification received\n[lastSync = notification.Timestamp]
    
    note right of Current
        VolumeCache matches CommonVolumeCache
        lastSync >= commonTimestamp
    end note
    
    note left of Stale
        CommonVolumeCache has newer data
        Sync needed before display
    end note
```

### Sync Decision Matrix

| Focus Level | Timestamp Comparison | Action |
|-------------|---------------------|--------|
| IN_FOCUS | N/A | Push notification updates lastSync |
| NOT_IN_FOCUS → IN_FOCUS | lastSync < commonTimestamp | Pull data, update cache |
| NOT_IN_FOCUS → IN_FOCUS | lastSync >= commonTimestamp | No sync needed |
| NOT_IN_FOCUS | Any change | No action (defer to focus switch) |

---

## 7. Write Operation States

**Requirements:** US-005

```mermaid
stateDiagram-v2
    direction TB
    
    [*] --> Received : Write() called
    
    Received --> Validating : Check parameters
    
    Validating --> StoringData : Valid
    Validating --> Failed : Invalid parameters
    
    StoringData --> UpdatingTimestamps : Cache store success
    
    UpdatingTimestamps --> FindingSubscribers : Timestamps updated
    
    FindingSubscribers --> Notifying : Subscribers found
    FindingSubscribers --> Complete : No subscribers
    
    Notifying --> Complete : Async dispatch started
    
    Complete --> [*]
    Failed --> [*]
    
    note right of Complete
        Write returns here
        Notifications continue async
    end note
    
    note right of Notifying
        Fire-and-forget
        Task.Run per subscriber
    end note
```

---

## 8. Subscription Registry Concurrency States

**Requirements:** US-019

```mermaid
stateDiagram-v2
    direction TB
    
    state RegistryOperations {
        [*] --> Idle
        
        Idle --> ReadLock : Query operation
        Idle --> WriteLock : Modify operation
        
        ReadLock --> Idle : Read complete
        WriteLock --> Idle : Write complete
        
        state ReadLock {
            GetByVolume
            GetByApp
            GetMatchingSubscribers
        }
        
        state WriteLock {
            Add
            Remove
            UpdateFocus
        }
    }
    
    note right of ReadLock
        ConcurrentDictionary allows
        concurrent reads
    end note
    
    note right of WriteLock
        Lock on specific list
        for safe modification
    end note
```

---

## State Transition Summary

### Quick Reference Table

| Entity | States | Key Transitions |
|--------|--------|-----------------|
| **Subscription** | Unsubscribed → InFocus/NotInFocus | Subscribe, Unsubscribe, ChangeFocus |
| **Notification** | Created → Queued → Delivering → Completed | Automatic on Write |
| **Cache Entry** | Empty → Present → Deleted | Created, Updated, Deleted |
| **Focus Level** | InFocus ↔ NotInFocus | ChangeFocus (atomic) |
| **Data Sync** | Current ↔ Stale | Cache update / Focus switch |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-03-23 | | Initial state diagrams |

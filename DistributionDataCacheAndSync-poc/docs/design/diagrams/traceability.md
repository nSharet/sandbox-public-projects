# Design Traceability: Distribution Data Cache and Sync Framework

**Date:** 2026-03-23
**Version:** 1.1

---

## Overview

This document provides traceability from detailed design decisions back to brainstorming, requirements, and architecture documents.

---

## 1. Approach Traceability

### Selected Approach: Two-Tier Subscription (Focus + Background)

| Source | Reference | Decision |
|--------|-----------|----------|
| Brainstorming | Approach 2 | Two-tier subscription model selected |
| Brainstorming | Direction 3 | Hybrid Push-Pull with Focus Levels |
| Decision Log | 2026-03-22 | "Recommend Two-Tier Subscription model" |

**Why Selected (from Brainstorming):**
- Best balance of real-time for active + efficiency for inactive
- Reduces notification processing for background volumes
- Apps control their own focus state
- Graceful handling of many open volumes

**Alternatives Rejected:**

| Approach | Decision | Rationale |
|----------|----------|-----------|
| Full Subscription with Real-Time Push | Rejected | "Would flood notifications for inactive volumes" |
| Pull-On-Switch Only | Rejected | "Fails real-time requirement" |
| Event Sourcing | Rejected | "Over-engineering for this problem" |

---

## 2. Component Traceability

### 2.1 ICommonVolumeCacheManager

| Design Element | Requirement | Architecture | Brainstorming |
|----------------|-------------|--------------|---------------|
| `Subscribe()` | US-006, US-007 | Key Interfaces | "Subscription Manager knows exactly who is interested" |
| `Unsubscribe()` | US-008 | Key Interfaces | "Support subscribe/unsubscribe" |
| `ChangeFocus()` | US-009, US-013 | Key Interfaces | "In Focus vs Not In Focus" |
| `Write()` | US-005, US-010 | Key Interfaces | "Write path: App → Private → Common" |
| `Read()` | US-013 | Key Interfaces | "Pull on focus switch" |

### 2.2 SubscriptionRegistry

| Design Element | Requirement | Architecture | Brainstorming |
|----------------|-------------|--------------|---------------|
| `Add(entry)` | US-006 | ISubscriptionRegistry | "Track subscriptions" |
| `GetMatchingSubscribers()` | US-015 | ISubscriptionRegistry | "Efficient lookup for notifications" |
| Hierarchical matching | US-006 (AC-006.1) | Architecture §5 | "Aspect-level + Volume-level matching" |

### 2.3 CommonVolumeCachePublisher

| Design Element | Requirement | Architecture | Brainstorming | ADR |
|----------------|-------------|--------------|---------------|-----|
| Async fire-and-forget | US-011 | ICommonVolumeCachePublisher | "One app's failure does NOT prevent others" | ADR-0001 |
| `Task.Run()` per subscriber | US-011 (AC-011.1) | Thread Safety | "Each receives via independent async task" | ADR-0001 |
| Exclude triggering app | US-012 | Notification flow | "No cyclic notifications" | - |

### 2.4 Generator (Background Pre-Loading)

| Design Element | Requirement | Rationale |
|----------------|-------------|-----------|
| `IGenerator` interface | Infrastructure | Pre-load data before app launches |
| `PreRegisterApplication()` | US-006, US-007 | Subscribe as NOT_IN_FOCUS on behalf of app |
| `PreLoadDataAsync()` | Infrastructure | Populate VolumeCache before app loads |
| `StartAsync()` | Infrastructure | Background polling/data source monitoring |
| Generator as writer (`GeneratorId`) | US-012 | Exclude generator from self-notifications |

**Pattern:**
- Generator subscribes for apps as `NOT_IN_FOCUS`
- When app launches, calls `ChangeFocusAll()` to upgrade to `IN_FOCUS`
- Pre-loaded cache means instant startup with no sync needed

### 2.5 FocusLevel Enum

| Design Element | Requirement | Brainstorming |
|----------------|-------------|---------------|
| `InFocus` | US-006 | "Real-time push notifications" |
| `NotInFocus` | US-007 | "Pull on focus switch" |

---

## 3. Data Structure Traceability

### 3.1 VolumeAspect Enum

| Design Element | Decision Source | Rationale |
|----------------|-----------------|-----------|
| Framework-defined enum | Architecture v1.3 | "Type-safe; no string parsing" |
| Tissue, AnatomicalPath, TBD | Architecture | "Framework-defined aspects" |

**Architecture Notes:**
- "VolumeId + VolumeAspect enum: Type-safe; no string parsing; framework-defined aspects"

### 3.2 IVolumeAspectChange Interface

| Design Element | Decision Source | Rationale |
|----------------|-----------------|-----------|
| Type-safe change classes | Architecture v1.3 | "Enables pattern matching" |
| All include full data | Architecture v1.3 | "TissueDeleted still has Tissue property" |

**From Architecture §Data Structures:**
- "Type-safe change classes; all include full data"
- Enables: `case TissueDeleted td => RemoveFromCache(td.Tissue.TissueId)`

### 3.3 SubscriptionKey

| Design Element | Decision Source | Rationale |
|----------------|-----------------|-----------|
| Composite (VolumeId, Aspect?) | Architecture v1.1 | "Replaced string topic" |
| Aspect nullable | Architecture v1.2 | "null = subscribe to ALL aspects" |

---

## 4. Flow Traceability

### 4.1 Write Path

| Flow Step | Requirement | Architecture Ref |
|-----------|-------------|------------------|
| Store in cache | US-005 (AC-005.2) | Write Path diagram |
| Update timestamps (aspect + volume) | US-014 (AC-014.2) | Timestamp propagation |
| Find IN_FOCUS subscribers | US-015 | GetMatchingSubscribers |
| Exclude writer | US-012 | No Cyclic Notifications |
| Async notify | US-011 | ADR-0001 |

### 4.2 Focus Switch Path

| Flow Step | Requirement | Architecture Ref |
|-----------|-------------|------------------|
| UpdateFocus in registry | US-009 (AC-009.4) | ChangeFocus sequence |
| Compare timestamps | US-013 (AC-013.1) | Focus Switch diagram |
| Sync if stale | US-013 (AC-013.2, AC-013.3) | Focus Switch diagram |
| Update LastSync | US-013 (AC-013.4) | SubscriptionEntry |

### 4.3 Subscribe Flow

| Flow Step | Requirement | Architecture Ref |
|-----------|-------------|------------------|
| Validate parameters | US-006 | API Contracts |
| Check for duplicate | US-006 (AC-006.4) | Idempotent |
| Add to registry | US-006 (AC-006.2) | SubscriptionRegistry |
| Immediate registration | US-006 (AC-006.5) | Synchronous |

---

## 5. Architecture Decision Traceability

### ADR-0001: Async Fire-and-Forget Notifications

| Related Design | Implementation |
|----------------|----------------|
| `CommonVolumeCachePublisher.NotifyAsync()` | Uses `Task.Run()` per subscriber |
| Error handling | Log and continue, don't rethrow |
| Independence | One failure doesn't block others |

**Linked Requirements:** US-011 (AC-011.1 to AC-011.5)

### ADR-0002: Registry-Based Subscriptions (No Events)

| Related Design | Implementation |
|----------------|----------------|
| `ISubscriptionRegistry` interface | Explicit Add/Remove methods |
| `SubscriptionEntry` | Contains AppId for filtering |
| No C# `event` keyword | Action delegates stored in registry |

**Why Not Events (from ADR):**
- Events don't carry subscriber identity (can't filter self-notifications)
- Multicast delegates complicate per-subscriber error handling
- Can't associate rich metadata (topic, focus level) with event handlers

**Linked Requirements:** US-012 (self-notification filtering)

---

## 6. Non-Functional Requirements Traceability

### Performance Targets

| Target | Source | Design Implementation |
|--------|--------|----------------------|
| <100ms notification delivery | Requirements §10 | Async Task.Run(), ConcurrentDictionary |
| <500ms focus switch | Requirements §10 | Simple timestamp compare, minimal data |
| O(1) subscriber lookup | US-015 | ConcurrentDictionary by key |

### Thread Safety

| Requirement | Design Implementation |
|-------------|----------------------|
| Concurrent read/write | ConcurrentDictionary for all stores |
| Atomic subscription changes | Lock on specific list for modifications |
| Independent notifications | Task.Run() isolates each callback |

---

## 7. User Story Coverage

### Coverage Matrix

| Epic | User Stories | Diagrams | API Methods |
|------|--------------|----------|-------------|
| Epic 1: Cache Architecture | US-001, US-002 | Class Diagrams | N/A (infrastructure) |
| Epic 2: Data Conversion | US-003, US-004 | Sequence Diagrams | N/A (app responsibility) |
| Epic 3: Write Path | US-005 | Sequence: Write Flow | `Write()` |
| Epic 4: Subscription | US-006 to US-009 | Sequence: Subscribe/Unsubscribe | `Subscribe()`, `Unsubscribe()`, `ChangeFocus()` |
| Epic 5: In Focus | US-010 to US-012 | Sequence: Write with Notifications | `NotifyAsync()` |
| Epic 6: Focus Switch | US-013, US-014 | Sequence: Focus Switch | `ChangeFocus()`, `Read()` |
| Epic 7: Registry | US-015, US-016 | Class: SubscriptionRegistry | `GetMySubscriptions()` |
| Epic 8: Error Handling | US-017, US-018 | State: Notification Lifecycle | Logging, try-catch |
| Epic 9: Race Conditions | US-019 | State: Concurrency | Lock strategy |

---

## 8. Design Decision Summary

| Decision Domain | Selected Option | Brainstorming Ref | Architecture Ref |
|-----------------|-----------------|-------------------|------------------|
| Subscription Model | Two-Tier (Focus + Background) | Approach 2 | System Context |
| Notification Delivery | Async Fire-and-Forget | Direction 1 + ADR-0001 | ADR-0001 |
| Subscription Management | Registry with Action delegates | Not C# events | ADR-0002 |
| Topic Identification | VolumeId + VolumeAspect enum | - | Data Structures |
| Change Payload | Full data always included | - | IVolumeAspectChange |
| Sync Strategy | Timestamp comparison | Approach 3 elements | Focus Switch Flow |

---

## 9. Open Items

| Item | Status | Design Impact |
|------|--------|---------------|
| VolumeAspect.TBD placeholder | Pending | May add more aspects |
| Retry mechanism for failed notifications | Optional (US-017.4) | Could add to CommonVolumeCachePublisher |
| Historical change tracking | Nice-to-have | Not in detailed design |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-03-23 | | Initial traceability matrix || 1.1 | 2026-03-23 | | Added Generator (section 2.4) traceability |
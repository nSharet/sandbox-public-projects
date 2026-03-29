# Requirements: Distribution Data Cache and Sync Framework

**Date:** 2026-03-22
**Version:** 1.2
**Status:** Draft
**Brainstorming Reference:** [brainstorming.md](brainstorming.md)
**Chosen Approach:** Two-Tier Subscription (Focus + Background)

---

## Overview

This document defines the requirements for the Distribution Data Cache and Sync Framework, implementing a two-tier subscription model with:
- **VolumeCaches** per application for app-specific objects
- **CommonVolumeCache** for shared data objects
- **In Focus** subscriptions with async push notifications
- **Not In Focus** subscriptions with pull-on-switch
- **Aspect-based granular subscriptions** for fine-grained notification filtering

---

## Stakeholders

| Role | Interest | Priority |
|------|----------|----------|
| Application Teams | Need efficient cache access and notifications | High |
| End Users | Expect real-time updates on active views | High |
| Platform Team | Owns cache infrastructure and framework | High |
| DevOps | Monitoring, performance, scalability | Medium |

---

## Functional Requirements

### Epic 1: Cache Architecture

#### US-001: VolumeCache per Application
**As an** application developer
**I want** my application to have its own VolumeCache
**So that** I can store app-specific objects without polluting other apps' caches

**Acceptance Criteria:**
- [ ] **AC-001.1**: Each application has an isolated VolumeCache instance
- [ ] **AC-001.2**: VolumeCache stores app-specific object format (not common format)
- [ ] **AC-001.3**: One application cannot access another application's VolumeCache
- [ ] **AC-001.4**: VolumeCache is keyed by Volume ID
- [ ] **AC-001.5**: VolumeCache persists for the application's lifecycle

**Priority:** Must Have
**Estimate:** M

---

#### US-002: CommonVolumeCache for Shared Data
**As a** system
**I want** a centralized CommonVolumeCache for shared data objects
**So that** all applications share the same source of truth

**Acceptance Criteria:**
- [ ] **AC-002.1**: CommonVolumeCache stores only common data objects (no app-specific objects)
- [ ] **AC-002.2**: CommonVolumeCache is keyed by Volume ID
- [ ] **AC-002.3**: All applications can read from CommonVolumeCache
- [ ] **AC-002.4**: All applications can write to CommonVolumeCache (through conversion)
- [ ] **AC-002.5**: CommonVolumeCache tracks last-modified timestamp per volume

**Priority:** Must Have
**Estimate:** M

---

### Epic 2: Data Conversion

#### US-003: App-to-Common Conversion
**As an** application
**I want** to convert my app-specific objects to common format
**So that** other applications can understand the shared data

**Acceptance Criteria:**
- [ ] **AC-003.1**: Each application implements its own converter (app-specific)
- [ ] **AC-003.2**: Conversion is deterministic (same input always produces same output)
- [ ] **AC-003.3**: Conversion occurs when writing to CommonVolumeCache
- [ ] **AC-003.4**: Conversion failure prevents CommonVolumeCache update and logs error
- [ ] **AC-003.5**: Original app-specific object remains in VolumeCache regardless of conversion result

**Priority:** Must Have
**Estimate:** M

---

#### US-004: Common-to-App Conversion
**As an** application
**I want** to convert common format objects to my app-specific format
**So that** I can work with data in my native structure

**Acceptance Criteria:**
- [ ] **AC-004.1**: Conversion occurs when reading from CommonVolumeCache
- [ ] **AC-004.2**: Converted objects are stored in VolumeCache
- [ ] **AC-004.3**: Conversion failure results in no data (not partial data)
- [ ] **AC-004.4**: Conversion failure is logged with volume ID and error details

**Priority:** Must Have
**Estimate:** M

---

### Epic 3: Write Path

#### US-005: Write to Private and CommonVolumeCache
**As an** application
**I want** my updates to be stored in both private and CommonVolumeCache
**So that** my changes are persisted and shared with other applications

**Acceptance Criteria:**
- [ ] **AC-005.1**: GIVEN an app updates an object, WHEN save is called, THEN VolumeCache is updated first
- [ ] **AC-005.2**: GIVEN VolumeCache is updated, WHEN conversion succeeds, THEN CommonVolumeCache is updated
- [ ] **AC-005.3**: GIVEN CommonVolumeCache is updated, THEN volume timestamp is updated
- [ ] **AC-005.4**: Write operation includes metadata: triggeredBy (app ID), timestamp, volume ID
- [ ] **AC-005.5**: Write to CommonVolumeCache is atomic (single operation)

**Priority:** Must Have
**Estimate:** L

---

### Epic 4: Subscription Management

#### US-006: Subscribe to Topic (In Focus)
**As an** application
**I want** to subscribe to a topic (volume or volume:aspect) with "In Focus" priority
**So that** I receive real-time notifications for that topic

**Acceptance Criteria:**
- [ ] **AC-006.1**: Application can subscribe to topics as IN_FOCUS (volume or volume:aspect level)
- [ ] **AC-006.2**: Subscription is registered in the Subscription Registry
- [ ] **AC-006.3**: Subscription includes: app ID, topic, focus level (IN_FOCUS)
- [ ] **AC-006.4**: Duplicate subscriptions are idempotent (no error, updates focus level)
- [ ] **AC-006.5**: Subscription is immediate (no async delay to register)

**Priority:** Must Have
**Estimate:** M

**Examples:**
```
// Volume-level subscription - gets ALL changes to Volume1
Subscribe("Volume1", IN_FOCUS)

// Aspect-level subscription - gets ONLY Tissue changes
Subscribe("Volume1:Tissue", IN_FOCUS)
```

---

#### US-007: Subscribe to Topic (Not In Focus)
**As an** application
**I want** to track topics (volume or volume:aspect) I have open but not presenting
**So that** I can sync only relevant data when I switch to them

**Acceptance Criteria:**
- [ ] **AC-007.1**: Application can register topics as NOT_IN_FOCUS (volume or volume:aspect level)
- [ ] **AC-007.2**: NOT_IN_FOCUS topics are tracked with last-sync timestamp per topic
- [ ] **AC-007.3**: NOT_IN_FOCUS topics do NOT receive push notifications
- [ ] **AC-007.4**: Application can have multiple NOT_IN_FOCUS topics
- [ ] **AC-007.5**: Granularity matches IN_FOCUS subscription model (same pub/sub hierarchy)

**Priority:** Must Have
**Estimate:** S

**Examples:**
```
// Volume-level NOT_IN_FOCUS (sync all on switch)
Subscribe("Volume1", NOT_IN_FOCUS)

// Aspect-level NOT_IN_FOCUS (sync only Tissue on switch)
Subscribe("Volume1:Tissue", NOT_IN_FOCUS)
```

---

#### US-008: Unsubscribe from Topic
**As an** application
**I want** to unsubscribe from a topic (volume or volume:aspect)
**So that** I no longer receive notifications or track it

**Acceptance Criteria:**
- [ ] **AC-008.1**: Application can unsubscribe from any topic (volume or volume:aspect)
- [ ] **AC-008.2**: Unsubscribe removes entry from Subscription Registry
- [ ] **AC-008.3**: Unsubscribe for non-subscribed topic is idempotent (no error)
- [ ] **AC-008.4**: In-flight notifications for unsubscribed topic are handled gracefully
- [ ] **AC-008.5**: Unsubscribe("Volume1") does NOT auto-remove "Volume1:Tissue" (explicit only)

**Priority:** Must Have
**Estimate:** S

---

#### US-009: Change Focus Level
**As an** application
**I want** to change a topic's focus level (In Focus ↔ Not In Focus)
**So that** I can switch which topic gets real-time updates

**Acceptance Criteria:**
- [ ] **AC-009.1**: GIVEN app is IN_FOCUS for T1 and switches to T2, WHEN switch occurs, THEN T1 becomes NOT_IN_FOCUS
- [ ] **AC-009.2**: GIVEN app is IN_FOCUS for T1 and switches to T2, WHEN switch occurs, THEN T2 becomes IN_FOCUS
- [ ] **AC-009.3**: Focus change triggers sync for new IN_FOCUS topic (see US-013)
- [ ] **AC-009.4**: Focus change is atomic (no intermediate state visible)
- [ ] **AC-009.5**: Topic can be volume-level or aspect-level (same granularity)

**Priority:** Must Have
**Estimate:** M

---

### Epic 5: Read Path - In Focus Notifications

#### US-010: Real-Time Notification for In Focus Subscribers
**As an** application subscribed to a topic IN_FOCUS
**I want** to be notified immediately when that topic changes
**So that** I can update my view in real-time

**Acceptance Criteria:**
- [ ] **AC-010.1**: GIVEN App Y is IN_FOCUS for topic T, WHEN App X writes to T, THEN App Y receives notification
- [ ] **AC-010.2**: Notification includes: topic, change data, triggered by (app ID), timestamp
- [ ] **AC-010.3**: Notification is delivered within 100ms of change (target)
- [ ] **AC-010.4**: Notification triggers async conversion and VolumeCache update
- [ ] **AC-010.5**: Subscription matching follows hierarchical rules (Epic 10)

**Priority:** Must Have
**Estimate:** L

---

#### US-011: Async Fire-and-Forget Notification Delivery
**As the** CommonVolumeCache Manager
**I want** to deliver notifications asynchronously and independently
**So that** one slow application does not block others

**Acceptance Criteria:**
- [ ] **AC-011.1**: Each IN_FOCUS subscriber receives notification via independent async task
- [ ] **AC-011.2**: Main thread returns immediately after queuing async tasks (non-blocking)
- [ ] **AC-011.3**: One app's slow processing does NOT delay other apps' notifications
- [ ] **AC-011.4**: One app's failure does NOT prevent other apps from being notified
- [ ] **AC-011.5**: Failed notifications are logged with app ID, volume ID, error

**Priority:** Must Have
**Estimate:** M

**ADR Reference:** [ADR-0001](adr/0001-async-notification-execution-model.md)

---

#### US-012: No Cyclic Notifications
**As an** application
**I want** to NOT be notified about my own changes
**So that** I don't process updates I just made

**Acceptance Criteria:**
- [ ] **AC-012.1**: GIVEN App X writes to topic T, WHEN notifications are sent, THEN App X is excluded
- [ ] **AC-012.2**: Filtering uses the triggeredBy field from the write operation
- [ ] **AC-012.3**: Self-notification is never delivered (not delivered then discarded)

**Priority:** Must Have
**Estimate:** S

---

### Epic 6: Read Path - Focus Switch (Pull)

#### US-013: Sync on Focus Switch
**As an** application switching to a different topic
**I want** to pull only the changes relevant to my subscription
**So that** I see the latest data without syncing unnecessary aspects

**Acceptance Criteria:**
- [ ] **AC-013.1**: GIVEN App switches to topic, WHEN topic was NOT_IN_FOCUS, THEN compare timestamps for that topic
- [ ] **AC-013.2**: GIVEN CommonVolumeCache timestamp > VolumeCache lastSync for topic, THEN fetch only that topic's data
- [ ] **AC-013.3**: Fetched data is converted to app format and stored in VolumeCache
- [ ] **AC-013.4**: lastSync timestamp is updated per topic after successful sync
- [ ] **AC-013.5**: Sync completes within 500ms (target)
- [ ] **AC-013.6**: Volume-level subscription syncs all aspects; aspect-level syncs only that aspect

**Priority:** Must Have
**Estimate:** M

**Examples:**
```
// App subscribed to Volume1 (volume-level) - syncs ALL aspects
SwitchToFocus("Volume1") → Sync Volume1 entirely

// App subscribed to Volume1:Tissue (aspect-level) - syncs ONLY Tissue
SwitchToFocus("Volume1:Tissue") → Sync only Tissue data for Volume1
```

---

#### US-014: Timestamp Tracking per Topic
**As the** system
**I want** to track when each topic (volume or aspect) was last modified
**So that** applications can detect if they need to sync at the right granularity

**Acceptance Criteria:**
- [ ] **AC-014.1**: CommonVolumeCache maintains lastModified timestamp per topic (volume AND aspect level)
- [ ] **AC-014.2**: Write to "Volume1:Tissue" updates timestamp for BOTH "Volume1" AND "Volume1:Tissue"
- [ ] **AC-014.3**: VolumeCache maintains lastSync timestamp per app per subscribed topic
- [ ] **AC-014.4**: Timestamps use consistent format (UTC, millisecond precision)
- [ ] **AC-014.5**: Timestamp comparison respects subscription granularity

**Priority:** Must Have
**Estimate:** M

**Timestamp Propagation:**
```
Write("Volume1:Tissue"):
  → Update timestamp["Volume1:Tissue"] = now
  → Update timestamp["Volume1"] = now  // Parent also updated

Subscriber to "Volume1" sees: timestamp changed
Subscriber to "Volume1:Tissue" sees: timestamp changed
Subscriber to "Volume1:Geometry" sees: NO change
```

---

### Epic 7: Subscription Registry

#### US-015: Query Subscribers for Topic
**As the** CommonVolumeCache Manager
**I want** to quickly query who is subscribed to a topic
**So that** I can notify the right applications

**Acceptance Criteria:**
- [ ] **AC-015.1**: Query returns all IN_FOCUS subscribers for a topic in O(1) time
- [ ] **AC-015.2**: Query handles hierarchical matching (volume subscribers + aspect subscribers)
- [ ] **AC-015.3**: Query handles empty subscriber list (no error)
- [ ] **AC-015.4**: Registry supports concurrent read/write access

**Priority:** Must Have
**Estimate:** M

---

#### US-016: List Subscriptions for Application
**As an** application
**I want** to see what topics I'm subscribed to
**So that** I can manage my subscriptions

**Acceptance Criteria:**
- [ ] **AC-016.1**: Application can query its own subscriptions
- [ ] **AC-016.2**: Query returns list of topics with focus level
- [ ] **AC-016.3**: Application CANNOT query other apps' subscriptions

**Priority:** Should Have
**Estimate:** S

---

### Epic 8: Error Handling

#### US-017: Handle Notification Delivery Failure
**As the** system
**I want** to gracefully handle notification failures
**So that** the system remains stable

**Acceptance Criteria:**
- [ ] **AC-017.1**: Failed notification is logged with full context
- [ ] **AC-017.2**: Failed notification does NOT affect other notifications
- [ ] **AC-017.3**: Failed notification does NOT rollback the original write
- [ ] **AC-017.4**: Optional: Failed notifications can be retried (configurable)

**Priority:** Must Have
**Estimate:** M

---

#### US-018: Handle Conversion Failure
**As an** application
**I want** conversion failures to be handled gracefully
**So that** partial data doesn't corrupt my cache

**Acceptance Criteria:**
- [ ] **AC-018.1**: Conversion failure on write: VolumeCache updated, CommonVolumeCache NOT updated, error logged
- [ ] **AC-018.2**: Conversion failure on read: VolumeCache NOT updated, error logged
- [ ] **AC-018.3**: Conversion errors include app ID, volume ID, error details

**Priority:** Must Have
**Estimate:** S

---

### Epic 9: Race Condition Handling

#### US-019: Handle Concurrent Subscribe/Unsubscribe
**As the** system
**I want** to handle race conditions on subscription changes
**So that** notifications are delivered correctly even during focus changes

**Acceptance Criteria:**
- [ ] **AC-019.1**: Subscription changes are atomic
- [ ] **AC-019.2**: In-flight notification arriving after unsubscribe is discarded (no error)
- [ ] **AC-019.3**: Subscribe during notification processing is handled correctly

**Priority:** Must Have
**Estimate:** M

---

#### US-020: Handle Notification During Focus Switch
**As an** application
**I want** notifications arriving during focus switch to be handled correctly
**So that** I don't miss or duplicate updates

**Acceptance Criteria:**
- [ ] **AC-020.1**: Application checks if still IN_FOCUS before processing notification
- [ ] **AC-020.2**: Notification for old focus volume is safely discarded
- [ ] **AC-020.3**: Post-switch sync includes any changes that arrived during switch

**Priority:** Should Have
**Estimate:** M

---

### Epic 10: Hierarchical Subscription Model (Pub/Sub)

> **New in v1.1:** Pub/Sub subscription model where applications choose their subscription granularity.

**Key Principle:** The CommonVolumeCache Manager is a **dumb broker** - it only performs subscription matching. Full responsibility for choosing subscription level is on the **application**.

#### Subscription Hierarchy

```
Subscription Levels:
├── Volume Level (Category)
│   └── Subscribe to Volume1 → Get ALL updates for Volume1
│
└── Aspect Level (Topic under Category)
    └── Subscribe to Volume1:Tissue → Get ONLY Tissue updates for Volume1
```

**Analogy:** Like a magazine subscription:
- Subscribe to "Sports" category → Get all sports articles
- Subscribe to "Sports:Football" topic → Get only football articles

---

#### US-021: Hierarchical Subscription Registration
**As an** application
**I want** to subscribe at either volume level or aspect level
**So that** I control the granularity of notifications I receive

**Acceptance Criteria:**
- [ ] **AC-021.1**: App can subscribe to `Volume1` (gets ALL changes to Volume1)
- [ ] **AC-021.2**: App can subscribe to `Volume1:Tissue` (gets ONLY Tissue changes)
- [ ] **AC-021.3**: App can subscribe to `Volume1:AnatomicalPath` (gets ONLY AnatomicalPath changes)
- [ ] **AC-021.4**: App can have multiple subscriptions (e.g., `Volume1:Tissue` + `Volume1:Geometry`)
- [ ] **AC-021.5**: Framework stores subscription exactly as provided (no interpretation)

**Priority:** Should Have
**Estimate:** M

**Examples:**
```
// Volume-level subscription (category)
Subscribe("Volume1", IN_FOCUS)  // Gets all Volume1 updates

// Aspect-level subscription (topic)
Subscribe("Volume1:Tissue", IN_FOCUS)  // Gets only Tissue updates
Subscribe("Volume1:AnatomicalPath", IN_FOCUS)  // Gets only AnatomicalPath updates
```

---

#### US-022: Topic-Based Write Notification
**As an** application writing data
**I want** to specify the topic (volume:aspect) of my change
**So that** only relevant subscribers are notified

**Acceptance Criteria:**
- [ ] **AC-022.1**: Write operation specifies topic: `Volume1:AnatomicalPath`
- [ ] **AC-022.2**: Framework extracts volume (`Volume1`) and aspect (`AnatomicalPath`) from topic
- [ ] **AC-022.3**: Framework notifies: (a) subscribers to `Volume1` AND (b) subscribers to `Volume1:AnatomicalPath`
- [ ] **AC-022.4**: Subscribers to `Volume1:Tissue` are NOT notified
- [ ] **AC-022.5**: Application provides topic; framework does not interpret or validate it

**Priority:** Should Have
**Estimate:** M

---

#### US-023: Simple Subscription Matching (Framework Responsibility)
**As the** CommonVolumeCache Manager
**I want** to perform simple subscription matching
**So that** I remain a dumb broker with no business logic

**Acceptance Criteria:**
- [ ] **AC-023.1**: When write topic = `V1:Aspect`, notify subscribers to `V1` AND `V1:Aspect`
- [ ] **AC-023.2**: Matching is string-based (no semantic interpretation)
- [ ] **AC-023.3**: Framework does NOT validate if aspect is "valid" (app's responsibility)
- [ ] **AC-023.4**: Framework does NOT care how many subscriptions an app has
- [ ] **AC-023.5**: All subscription management logic is O(1) lookup

**Priority:** Should Have
**Estimate:** S

**Framework Matching Logic:**
```
On Write(topic: "Volume1:AnatomicalPath"):
  1. Parse: volume = "Volume1", aspect = "AnatomicalPath"
  2. Notify: subscribers["Volume1"]           // Category subscribers
  3. Notify: subscribers["Volume1:AnatomicalPath"]  // Topic subscribers
  4. Skip:   subscribers["Volume1:Tissue"]    // Different topic
```

---

#### US-024: Application Owns Subscription Strategy
**As an** application
**I want** full control over my subscription strategy
**So that** the framework remains simple and I own the complexity

**Acceptance Criteria:**
- [ ] **AC-024.1**: Application decides: subscribe to volume OR specific aspects (framework doesn't guide)
- [ ] **AC-024.2**: Application decides: how many aspects to subscribe to (no framework limit)
- [ ] **AC-024.3**: Application decides: when to switch subscription granularity
- [ ] **AC-024.4**: Application provides topic on write (framework doesn't infer it)
- [ ] **AC-024.5**: Invalid/unknown topics are passed through (no framework validation)

**Priority:** Should Have
**Estimate:** S

---

**Summary: Responsibility Split**

| Responsibility | Owner |
|---------------|-------|
| Decide subscription granularity (volume vs aspect) | **Application** |
| Register/manage subscriptions | **Application** |
| Provide topic on write (volume:aspect) | **Application** |
| Define valid aspects | **Application/Data Model** |
| Perform subscription matching | **Framework** |
| Deliver notifications | **Framework** |
| Validate aspects/topics | **NOT Framework** |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target | Measurement |
|----|-------------|--------|-------------|
| **NFR-P01** | In Focus notification latency | < 100ms | Time from write to notification delivery |
| **NFR-P02** | Focus switch sync time | < 500ms | Time from switch to data ready |
| **NFR-P03** | VolumeCache read | < 10ms | Time to retrieve from VolumeCache |
| **NFR-P04** | Registry lookup | < 1ms | Time to query subscribers for volume |

### Scalability

| ID | Requirement | Target | Notes |
|----|-------------|--------|-------|
| **NFR-S01** | Applications supported | 10+ | Concurrent apps in process |
| **NFR-S02** | Volumes per app | 100+ | Open volumes (IN_FOCUS + NOT_IN_FOCUS) |
| **NFR-S03** | Subscribers per volume | 5+ | Concurrent IN_FOCUS subscribers |
| **NFR-S04** | Changes per second | 100+ | Sustained write rate |

### Reliability

| ID | Requirement | Target | Notes |
|----|-------------|--------|-------|
| **NFR-R01** | Notification delivery | 99.9% | For IN_FOCUS subscribers |
| **NFR-R02** | No data loss on failure | 100% | VolumeCache write must succeed |
| **NFR-R03** | Recovery from app crash | < 1s | Other apps unaffected |

### Observability

| ID | Requirement | Target | Notes |
|----|-------------|--------|-------|
| **NFR-O01** | Notification failure logging | Required | All failures logged with context |
| **NFR-O02** | Subscription metrics | Should Have | Count by app, volume, focus level |
| **NFR-O03** | Latency metrics | Should Have | P50, P95, P99 for notifications |

---

## Out of Scope

The following are explicitly **NOT** part of this implementation:

| Item | Reason |
|------|--------|
| Cross-process cache sync | Current scope is in-process only |
| Historical change replay | Not required; pull-on-switch is sufficient |
| Notification ordering guarantees | Async notifications may arrive out of order |
| CommonVolumeCache persistence | Cache is in-memory, not persisted |
| Multi-tenant security | All apps are trusted within the process |
| Real-time sync for ALL volumes | Only IN_FOCUS volumes get real-time |

---

## Dependencies

| Dependency | Description | Owner |
|------------|-------------|-------|
| Existing cache infrastructure | May need migration/adaptation | Platform Team |
| Application converters | Each app must implement | App Teams |
| Focus state management | Apps must track their presentation state | App Teams |

---

## Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| App converter performance | High | Medium | Benchmark before integration; set timeout |
| Race conditions on focus switch | Medium | Medium | Defensive coding; logging; tests |
| Memory growth with many volumes | Medium | Low | Monitor; implement LRU eviction if needed |
| Async notification backlog | Low | Low | Monitor queue depth; add TTL if needed |

---

## Open Questions - Resolved

| Question | Answer | Rationale |
|----------|--------|----------|
| Should converters have a timeout? | **No** | Application's responsibility. Data model defined by common layer; all apps must align. |
| What is the maximum number of IN_FOCUS volumes per app? | **No limit** | Reality is ~10 apps with multiple volumes each. |
| Should subscription state persist across restart? | **No** | All managed in-memory only. Apps re-register on startup. |
| Should CommonVolumeCache Manager retry failed notifications? | **No** | Application's responsibility. Once notified, app handles retry logic. |
| Can focus switch be cancelled mid-operation? | **No** | App must complete accepting changes before triggering CommonVolumeCache. Internal flow uses other mechanisms. |
| Should NOT_IN_FOCUS updates be queued? | **No** | Just use timestamp comparison on focus switch. Simpler and sufficient. |

---

## Traceability

| Requirement | Brainstorming Reference |
|-------------|------------------------|
| US-001 (VolumeCache) | Success Criteria: "Each app has its own VolumeCache" |
| US-002 (CommonVolumeCache) | Success Criteria: "CommonVolumeCache contains only shared data" |
| US-010 (In Focus Notifications) | Approach 2: "In Focus: Real-time push notifications" |
| US-011 (Async Fire-and-Forget) | ADR-0001: "Async fire-and-forget execution model" |
| US-012 (No Cyclic) | Success Criteria: "No cyclic notifications" |
| US-013 (Sync on Switch) | Approach 2: "Not In Focus: Pull-on-switch" |
| US-021-024 (Hierarchical Subscription) | Pub/Sub pattern with volume/aspect hierarchy |

---

## Design Decisions Incorporated

| Decision | Implication |
|----------|-------------|
| No converter timeout | Converters must be fast; no framework safety net |
| No subscription persistence | Apps must re-register all subscriptions on startup |
| No notification retry | Apps own retry logic for processing failures |
| In-memory only | All caches cleared on process restart |
| **Framework is dumb broker** | Only performs subscription matching; no business logic |
| **App owns subscription strategy** | App decides volume-level vs aspect-level subscription |
| **No topic validation** | Framework passes through any topic string |
| **Unified topic model** | Both IN_FOCUS and NOT_IN_FOCUS use same granularity (volume or aspect) |
| **Hierarchical timestamps** | Write to aspect updates both aspect and volume timestamps |

---

## Sign-off

| Role | Name | Date | Approved |
|------|------|------|----------|
| Product Owner | | | [ ] |
| Tech Lead | | | [ ] |
| Principal Engineer | | | [ ] |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-03-22 | | Initial requirements based on brainstorming |
| 1.1 | 2026-03-22 | | Added Epic 10: Hierarchical Subscriptions. Resolved open questions. |
| 1.2 | 2026-03-22 | | Extended hierarchical model to NOT_IN_FOCUS. Unified "topic" terminology throughout. |
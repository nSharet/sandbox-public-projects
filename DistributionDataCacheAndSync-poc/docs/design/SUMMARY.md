# Distribution Data Cache & Sync - Executive Summary

> For design review presentations and stakeholder communication

---

## Problem Statement

| Current State | Impact |
|--------------|--------|
| Single shared cache for all apps | Cache pollution with app-specific data |
| All apps notified on any change | Unnecessary processing, performance issues |
| No selective volume interest | Apps react to irrelevant changes |

```mermaid
flowchart LR
    subgraph "Current: Over-Notification"
        Change[Any Change] -->|Notifies ALL| AppX
        Change -->|Notifies ALL| AppY
        Change -->|Notifies ALL| AppZ
    end
```

---

## Goal

| Requirement | Description |
|-------------|-------------|
| VolumeCaches | Each app owns its cache for app-specific objects |
| CommonVolumeCache | Shared data only - no app-specific pollution |
| Smart notifications | Only notify apps presenting the changed volume |
| No cyclic updates | App that triggers change is not notified back |

---

## Approaches Evaluated

| # | Approach | Description | Score | Verdict |
|---|----------|-------------|-------|---------|
| 1 | **Full Push Subscription** | All subscribed apps get real-time push for every change | 159/200 | Good, but inefficient for many volumes |
| 2 | **Two-Tier Subscription** | Real-time for "In Focus" + lazy sync for "Not In Focus" | 155/200 | ✅ **Recommended** |
| 3 | **Pull-On-Switch** | No push - apps sync when switching volumes | 126/200 | ❌ Fails real-time requirement |
| 4 | **Event Sourcing** | Store all changes as events, apps replay | 103/200 | ❌ Over-engineering |

### Evaluation Criteria

| Criteria | Weight | Why It Matters |
|----------|--------|----------------|
| Meets must-have requirements | 5 | Non-negotiable |
| Real-time for active volume | 4 | Core UX requirement |
| Simplicity | 3 | Maintenance cost |
| Notification efficiency | 3 | Performance |
| Maintainability | 3 | Long-term cost |
| Migration effort | 2 | Transition risk |
| Team familiarity | 2 | Learning curve |

---

## Recommendation: Two-Tier Subscription

### Concept

```mermaid
flowchart TB
    subgraph "In Focus (Real-time)"
        Push[Immediate Push Notification]
    end
    
    subgraph "Not In Focus (Lazy)"
        Pull[Sync on Focus Switch]
    end
    
    Change[Volume Changed] --> Manager[CommonVolumeCache Manager]
    Manager -->|"Presenting this volume?"| Push
    Manager -->|"Not presenting?"| Pull
```

### How It Works

| Level | Execution | Behavior |
|-------|-----------|----------|
| **In Focus** | Async fire-and-forget from main thread | Each app notified independently, non-blocking |
| **Not In Focus** | Separate background task | Records timestamp; app pulls on focus switch |

### Execution Model (ADR-0001)

```mermaid
flowchart TB
    subgraph "Main Thread"
        Change[Volume Change] --> Store[Store in CommonVolumeCache]
        Store --> Fire["Fire Async Tasks<br>(non-blocking)"]
        Store --> Queue[Queue Background Task]
    end
    
    subgraph "Independent Async Tasks"
        Fire --> T1[Task: Notify App A]
        Fire --> T2[Task: Notify App B]
        T1 --> A1[App A updates]
        T2 --> A2[App B updates]
    end
    
    subgraph "Background"
        Queue --> TS[Record Timestamp]
    end
```

**Key Point:** Each In Focus notification is an **independent async task** - one slow/blocked app cannot delay others.

### Key Flow

```mermaid
sequenceDiagram
    participant AppX as App X (trigger)
    participant Main as Main Thread
    participant TaskY as Async Task
    participant AppY as App Y (In Focus V1)
    participant AppZ as App Z (Not In Focus)
    
    AppX->>Main: Update Volume 1
    Main->>Main: Store in CommonVolumeCache
    Main->>TaskY: Task.Run(() => NotifyY)
    Main->>Main: Return immediately
    
    TaskY->>AppY: Push notification
    AppY->>AppY: Convert & update
    
    Note over AppZ: Later: switches to V1
    AppZ->>Main: Pull changes since lastSync
    AppZ->>AppZ: Update registration
```

### Why This Approach?

| Factor | Reasoning |
|--------|-----------|
| **Real-time for active** | Users see changes immediately on presenting volume |
| **Non-blocking** | Async fire-and-forget prevents one app blocking others |
| **Efficient** | No wasted notifications to non-presenting apps |
| **Independent failures** | One app crash doesn't affect other notifications |
| **Incremental migration** | Can implement push first, add pull-on-switch later |

---

## Architecture Overview

```mermaid
flowchart TB
    subgraph "Per Application"
        App[Application]
        Private[(VolumeCache)]
        Converter[Converter]
        Focus[Focus State]
    end
    
    subgraph "Shared Infrastructure"
        Manager[CommonVolumeCache Manager]
        Common[(CommonVolumeCache)]
        Registry[Subscription Registry]
    end
    
    App --> Private
    App --> Focus
    Focus -->|"Subscribe IN_FOCUS"| Registry
    
    App --> Converter
    Converter -->|"Write"| Manager --> Common
    
    Manager -->|"Push"| App
    Common -->|"Pull on switch"| Converter --> Private
```

---

## Key Components

| Component | Responsibility |
|-----------|----------------|
| **VolumeCache** | App-specific objects, per application |
| **CommonVolumeCache** | Shared data objects only, keyed by Volume ID |
| **Subscription Registry** | Tracks who is IN_FOCUS for which volumes |
| **CommonVolumeCache Manager** | Orchestrates writes and notifications |
| **Converter** | App-specific: transforms common ↔ app format |
| **Focus State Manager** | Per-app: tracks which volumes are presented |

---

## Write & Read Paths

### Write Path
```
App updates object
    → Update VolumeCache
    → Convert to Common format
    → Update CommonVolumeCache
    → Notify IN_FOCUS subscribers (except trigger app)
```

### Read Path (on notification)
```
CommonVolumeCache changed
    → Manager checks Subscription Registry
    → For each IN_FOCUS app (except trigger):
        → Push notification
        → App reads Common
        → App converts to own format
        → App updates VolumeCache
        → App reflects in UI
```

### Sync Path (on focus switch)
```
App switches to Volume X
    → Compare timestamps (Common vs Private)
    → If stale: Fetch from Common, Convert, Update Private
    → Present to user
```

---

## POC Scope

| Aspect | Scope |
|--------|-------|
| **Apps** | 2 applications (X and Y) |
| **Volumes** | 3 test volumes |
| **Duration** | 2-3 weeks |
| **Include** | Registry, Focus Manager, Push path, Pull-on-switch |
| **Exclude** | Full migration, Performance tuning, Monitoring |

### Success Criteria

| Metric | Target |
|--------|--------|
| In Focus update latency | < 100ms |
| Focus switch sync time | < 500ms |
| Cyclic notifications | Zero |
| Race condition handling | Graceful |

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Two code paths | Medium | Clear separation, good test coverage |
| Focus state bugs | Low | State machine pattern, logging |
| Race on subscribe/unsubscribe | Medium | Locking or queue-based |

---

## Next Steps

1. [ ] Review with team
2. [ ] Validate assumptions with app teams
3. [ ] Approve approach
4. [ ] Define detailed requirements
5. [ ] Begin POC implementation

---

## References

- Full brainstorming: [brainstorming.md](brainstorming.md)
- Status tracking: [STATUS.md](STATUS.md)

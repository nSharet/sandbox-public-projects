# API Contracts: Distribution Data Cache and Sync Framework

**Date:** 2026-03-23
**Version:** 1.2
**Architecture Reference:** [architecture.md](../architecture.md)
**Requirements Reference:** [requirements.md](../requirements.md)

---

## Overview

This document defines the complete API contracts for the Distribution Data Cache and Sync Framework.

---

## 1. ICommonVolumeCacheManager

The primary interface for applications to interact with the framework.

### 1.1 Subscribe

Registers an application for notifications on a volume or volume aspect.

```csharp
/// <summary>
/// Subscribes an application to receive notifications for a volume or specific aspect.
/// </summary>
/// <param name="appId">Unique identifier for the subscribing application.</param>
/// <param name="volumeId">The volume identifier to subscribe to.</param>
/// <param name="aspect">
///     The specific aspect to subscribe to, or null for all aspects.
///     null = volume-level subscription (receives all aspect changes)
/// </param>
/// <param name="focus">The initial focus level (InFocus receives push, NotInFocus pulls on switch).</param>
/// <param name="callback">
///     The callback to invoke when notifications arrive.
///     Called asynchronously via Task.Run(), independent of other subscribers.
/// </param>
/// <exception cref="ArgumentNullException">If appId, volumeId, or callback is null.</exception>
/// <exception cref="ArgumentException">If appId or volumeId is empty or whitespace.</exception>
/// <remarks>
/// - Duplicate subscriptions are idempotent (updates focus level, replaces callback)
/// - Registration is immediate (synchronous)
/// - US-006, US-007
/// </remarks>
void Subscribe(
    string appId, 
    string volumeId, 
    VolumeAspect? aspect, 
    FocusLevel focus, 
    Action<NotificationData> callback);
```

**Examples:**
```csharp
// Subscribe to ALL aspects of Volume1
cacheManager.Subscribe("TissueViewer", "Volume1", null, FocusLevel.InFocus, OnNotification);

// Subscribe to ONLY Tissue changes
cacheManager.Subscribe("TissueViewer", "Volume1", VolumeAspect.Tissue, FocusLevel.InFocus, OnNotification);

// Background subscription (no push, pull on focus switch)
cacheManager.Subscribe("TissueViewer", "Volume2", null, FocusLevel.NotInFocus, OnNotification);
```

---

### 1.2 Unsubscribe

Removes a subscription for a volume or aspect.

```csharp
/// <summary>
/// Removes a subscription for an application from a volume or specific aspect.
/// </summary>
/// <param name="appId">The application identifier.</param>
/// <param name="volumeId">The volume identifier.</param>
/// <param name="aspect">
///     The specific aspect to unsubscribe from, or null for volume-level.
///     Must match the subscription exactly; unsubscribing from volume-level
///     does NOT remove aspect-level subscriptions.
/// </param>
/// <remarks>
/// - Idempotent: unsubscribing from non-existent subscription is a no-op
/// - In-flight notifications may still be delivered (app should handle gracefully)
/// - US-008
/// </remarks>
void Unsubscribe(string appId, string volumeId, VolumeAspect? aspect = null);
```

**Examples:**
```csharp
// Unsubscribe from volume-level subscription
cacheManager.Unsubscribe("TissueViewer", "Volume1", null);

// Unsubscribe from specific aspect
cacheManager.Unsubscribe("TissueViewer", "Volume1", VolumeAspect.Tissue);

// Note: These are DIFFERENT subscriptions!
```

---

### 1.2.1 UnsubscribeAll

Removes ALL subscriptions for an application (e.g., on app termination).

```csharp
/// <summary>
/// Removes all subscriptions for an application.
/// Typically called when an application is shutting down.
/// </summary>
/// <param name="appId">The application identifier.</param>
/// <returns>The number of subscriptions that were removed.</returns>
/// <remarks>
/// - Idempotent: calling on app with no subscriptions returns 0
/// - In-flight notifications may still be delivered (app should handle gracefully)
/// - Should be called during application shutdown/dispose
/// - US-008
/// </remarks>
int UnsubscribeAll(string appId);
```

**Examples:**
```csharp
// Application shutdown - remove all subscriptions
public void Dispose()
{
    var removedCount = cacheManager.UnsubscribeAll("TissueViewer");
    _logger.LogInformation("Removed {Count} subscriptions on shutdown", removedCount);
}
```

---

### 1.3 ChangeFocus

Changes the focus level of an existing subscription.

```csharp
/// <summary>
/// Changes the focus level of an existing subscription, optionally triggering a sync.
/// </summary>
/// <param name="appId">The application identifier.</param>
/// <param name="volumeId">The volume identifier.</param>
/// <param name="aspect">The aspect, or null for volume-level subscription.</param>
/// <param name="newFocus">The new focus level.</param>
/// <returns>
///     Sync data if switching to InFocus and data is stale; null otherwise.
///     Contains the full data from CommonVolumeCache and timestamp.
/// </returns>
/// <exception cref="InvalidOperationException">If no matching subscription exists.</exception>
/// <remarks>
/// - When switching to InFocus, automatically checks if sync is needed
/// - If sync needed, returns data; app should convert and update VolumeCache
/// - Focus change is atomic
/// - US-009, US-013
/// </remarks>
SyncResult? ChangeFocus(
    string appId, 
    string volumeId, 
    VolumeAspect? aspect, 
    FocusLevel newFocus);
```

**Return Type:**
```csharp
public class SyncResult
{
    /// <summary>The volume that was synced.</summary>
    public string VolumeId { get; init; }
    
    /// <summary>The data from CommonVolumeCache.</summary>
    public object Data { get; init; }
    
    /// <summary>The timestamp when this data was last modified.</summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>The aspect that was synced (null for volume-level).</summary>
    public VolumeAspect? Aspect { get; init; }
}
```

**Examples:**
```csharp
// Switch to foreground - may trigger sync
var syncResult = cacheManager.ChangeFocus("TissueViewer", "Volume1", null, FocusLevel.InFocus);
if (syncResult != null)
{
    var converted = converter.FromCommon(syncResult.Data);
    privateCache.Store(syncResult.Aspect, converted);
    privateCache.UpdateLastSync("Volume1", syncResult.Aspect, syncResult.Timestamp);
}

// Switch to background - never syncs
cacheManager.ChangeFocus("TissueViewer", "Volume1", null, FocusLevel.NotInFocus);
```

---

### 1.3.1 ChangeFocusAll

Changes the focus level of ALL subscriptions for an application.

```csharp
/// <summary>
/// Changes the focus level of all subscriptions for an application.
/// Useful when app is minimized/restored or loses/gains window focus.
/// </summary>
/// <param name="appId">The application identifier.</param>
/// <param name="newFocus">The new focus level to apply to all subscriptions.</param>
/// <returns>
///     List of sync results for subscriptions that need sync (when switching to InFocus).
///     Empty list if switching to NotInFocus or no data is stale.
/// </returns>
/// <remarks>
/// - Applies to ALL subscriptions regardless of volume or aspect
/// - When switching to InFocus, checks each subscription for staleness
/// - Returns sync data for any stale subscriptions; app should process all
/// - Focus changes are atomic per subscription
/// - US-009, US-013
/// </remarks>
IEnumerable<SyncResult> ChangeFocusAll(string appId, FocusLevel newFocus);
```

**Examples:**
```csharp
// App restored from minimized - switch all subscriptions to InFocus
protected override void OnActivated(EventArgs e)
{
    base.OnActivated(e);
    
    var syncResults = cacheManager.ChangeFocusAll("TissueViewer", FocusLevel.InFocus);
    foreach (var sync in syncResults)
    {
        var converted = converter.FromCommon(sync.Data);
        privateCache.Store(sync.VolumeId, sync.Aspect, converted);
        privateCache.UpdateLastSync(sync.VolumeId, sync.Aspect, sync.Timestamp);
    }
}

// App minimized - switch all to NotInFocus (no sync needed)
protected override void OnDeactivated(EventArgs e)
{
    base.OnDeactivated(e);
    cacheManager.ChangeFocusAll("TissueViewer", FocusLevel.NotInFocus);
}
```

---

### 1.4 GetMySubscriptions

Returns all subscriptions for an application.

```csharp
/// <summary>
/// Returns all current subscriptions for an application.
/// </summary>
/// <param name="appId">The application identifier.</param>
/// <returns>
///     Enumerable of subscription info objects.
///     Empty if no subscriptions exist.
/// </returns>
/// <remarks>
/// - Read-only; does not include the callback delegate
/// - US-016
/// </remarks>
IEnumerable<SubscriptionInfo> GetMySubscriptions(string appId);
```

**Return Type:**
```csharp
public class SubscriptionInfo
{
    public string VolumeId { get; init; }
    public VolumeAspect? Aspect { get; init; }
    public FocusLevel FocusLevel { get; init; }
    public DateTime SubscribedAt { get; init; }
    public DateTime LastSync { get; init; }
}
```

**Example:**
```csharp
var subs = cacheManager.GetMySubscriptions("TissueViewer");
foreach (var sub in subs)
{
    Console.WriteLine($"{sub.VolumeId}:{sub.Aspect} - {sub.FocusLevel}");
}
```

---

### 1.5 Write

Writes a change to the CommonVolumeCache and triggers notifications.

```csharp
/// <summary>
/// Writes a change to the CommonVolumeCache and notifies IN_FOCUS subscribers.
/// </summary>
/// <param name="appId">
///     The application making the change.
///     Used to prevent self-notification (cyclic updates).
/// </param>
/// <param name="volumeId">The volume being modified.</param>
/// <param name="change">
///     The type-safe change object containing full data.
///     Must implement IVolumeAspectChange.
/// </param>
/// <exception cref="ArgumentNullException">If any parameter is null.</exception>
/// <remarks>
/// - Stores data in CommonVolumeCache
/// - Updates timestamps for both aspect and volume level
/// - Notifies all IN_FOCUS subscribers asynchronously (except appId)
/// - Returns immediately; notifications are fire-and-forget
/// - US-005, US-010, US-011, US-012
/// </remarks>
void Write(string appId, string volumeId, IVolumeAspectChange change);
```

**Examples:**
```csharp
// Create new tissue
var tissue = new TissueData { TissueId = "T1", Name = "Liver", Color = "#8B0000" };
cacheManager.Write("TissueViewer", "Volume1", new TissueCreated { Tissue = tissue });

// Update existing tissue
var updated = new TissueData { TissueId = "T1", Name = "Liver", Color = "#FF0000" };
cacheManager.Write("TissueViewer", "Volume1", new TissueUpdated { Tissue = updated });

// Delete tissue (still includes full data!)
cacheManager.Write("TissueViewer", "Volume1", new TissueDeleted { Tissue = updated });
```

---

### 1.6 Read

Reads data from the CommonVolumeCache.

```csharp
/// <summary>
/// Reads data from the CommonVolumeCache for a specific volume or aspect.
/// </summary>
/// <param name="volumeId">The volume identifier.</param>
/// <param name="aspect">The specific aspect, or null to read all aspects.</param>
/// <returns>
///     Tuple of (data, timestamp) if data exists; null if no data.
///     For aspect=null, returns Dictionary&lt;VolumeAspect, object&gt;.
/// </returns>
/// <remarks>
/// - Used primarily during focus switch sync
/// - Does NOT convert data; app must convert after reading
/// </remarks>
(object Data, DateTime Timestamp)? Read(string volumeId, VolumeAspect? aspect = null);
```

**Example:**
```csharp
// Read specific aspect
var result = cacheManager.Read("Volume1", VolumeAspect.Tissue);
if (result.HasValue)
{
    var tissueData = result.Value.Data as TissueData;
    var timestamp = result.Value.Timestamp;
}

// Read all aspects
var allData = cacheManager.Read("Volume1", null);
if (allData.HasValue)
{
    var dataDict = allData.Value.Data as Dictionary<VolumeAspect, object>;
}
```

---

### 1.7 GetLastModified

Gets the last modification timestamp for a volume or aspect.

```csharp
/// <summary>
/// Gets the last modification timestamp for a volume or specific aspect.
/// </summary>
/// <param name="volumeId">The volume identifier.</param>
/// <param name="aspect">The specific aspect, or null for volume-level timestamp.</param>
/// <returns>The timestamp if data exists; null otherwise.</returns>
/// <remarks>
/// - Volume-level timestamp is updated when ANY aspect changes
/// - US-014
/// </remarks>
DateTime? GetLastModified(string volumeId, VolumeAspect? aspect = null);
```

---

## 2. ISubscriptionRegistry

Internal interface for managing subscriptions.

```csharp
public interface ISubscriptionRegistry
{
    /// <summary>Adds a new subscription entry.</summary>
    void Add(SubscriptionEntry entry);
    
    /// <summary>Removes a subscription.</summary>
    void Remove(string appId, string volumeId, VolumeAspect? aspect);
    
    /// <summary>Removes all subscriptions for an application.</summary>
    /// <returns>The number of subscriptions removed.</returns>
    int RemoveAll(string appId);
    
    /// <summary>Updates the focus level of a subscription.</summary>
    void UpdateFocus(string appId, string volumeId, VolumeAspect? aspect, FocusLevel newFocus);
    
    /// <summary>Updates the focus level of all subscriptions for an application.</summary>
    void UpdateFocusAll(string appId, FocusLevel newFocus);
    
    /// <summary>Gets subscriptions for a volume, optionally filtered by aspect and focus.</summary>
    IEnumerable<SubscriptionEntry> GetByVolume(
        string volumeId, 
        VolumeAspect? aspect = null, 
        FocusLevel? focus = null);
    
    /// <summary>Gets all subscriptions for an application.</summary>
    IEnumerable<SubscriptionEntry> GetByApp(string appId);
    
    /// <summary>
    /// Gets matching subscribers for a change, applying hierarchical matching.
    /// Returns both direct aspect subscribers AND volume-level subscribers.
    /// </summary>
    IEnumerable<SubscriptionEntry> GetMatchingSubscribers(
        string volumeId, 
        VolumeAspect aspect, 
        FocusLevel focus);
}
```

---

## 3. ICommonVolumeCachePublisher

Internal interface for delivering notifications.

```csharp
public interface ICommonVolumeCachePublisher
{
    /// <summary>
    /// Delivers notifications to subscribers asynchronously.
    /// Each subscriber receives notification in independent Task.Run().
    /// </summary>
    /// <param name="subscribers">The subscribers to notify.</param>
    /// <param name="data">The notification data.</param>
    /// <param name="excludeAppId">
    ///     App to exclude (the writer). Prevents self-notification.
    /// </param>
    /// <remarks>
    /// - Returns immediately (fire-and-forget)
    /// - Failures are logged but do not affect other subscribers
    /// - ADR-0001
    /// </remarks>
    void NotifyAsync(
        IEnumerable<SubscriptionEntry> subscribers, 
        NotificationData data, 
        string excludeAppId);
}
```

---

## 3.1 IGenerator

Interface for background generators that pre-load data for applications.

```csharp
public interface IGenerator
{
    /// <summary>
    /// Unique identifier for this generator.
    /// Used as appId when writing to CommonVolumeCache.
    /// </summary>
    string GeneratorId { get; }
    
    /// <summary>
    /// Gets the application IDs that this generator pre-loads data for.
    /// </summary>
    IEnumerable<string> TargetApplications { get; }
    
    /// <summary>
    /// Initializes the generator and starts background processing.
    /// Called once during framework startup.
    /// </summary>
    /// <param name="cacheManager">The CommonVolumeCache Manager instance.</param>
    /// <param name="cancellationToken">Token to cancel background operations.</param>
    Task StartAsync(ICommonVolumeCacheManager cacheManager, CancellationToken cancellationToken);
    
    /// <summary>
    /// Stops the generator and cleans up resources.
    /// Called during framework shutdown.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Pre-registers subscriptions for a target application.
    /// Called before the application is loaded by the user.
    /// </summary>
    /// <param name="appId">The target application identifier.</param>
    /// <param name="cacheManager">The CommonVolumeCache Manager.</param>
    /// <param name="callback">Callback to invoke when data changes (for pre-loaded cache updates).</param>
    void PreRegisterApplication(
        string appId, 
        ICommonVolumeCacheManager cacheManager,
        Action<NotificationData> callback);
    
    /// <summary>
    /// Pre-loads data into an application's VolumeCache.
    /// Called to populate cache before the application is loaded.
    /// </summary>
    /// <param name="appId">The target application identifier.</param>
    /// <param name="privateCache">The pre-created VolumeCache for the application.</param>
    Task PreLoadDataAsync(string appId, IPrivateCache privateCache);
}
```

**Example Implementation:**
```csharp
public class TissueDataGenerator : IGenerator
{
    public string GeneratorId => "TissueDataGenerator";
    public IEnumerable<string> TargetApplications => new[] { "TissueViewer", "AnatomyEditor" };
    
    public async Task StartAsync(ICommonVolumeCacheManager cacheManager, CancellationToken ct)
    {
        // Pre-register all target applications
        foreach (var appId in TargetApplications)
        {
            PreRegisterApplication(appId, cacheManager, OnDataChanged);
        }
        
        // Start background data polling/loading
        _ = Task.Run(() => PollDataSourceAsync(cacheManager, ct), ct);
    }
    
    public void PreRegisterApplication(
        string appId, 
        ICommonVolumeCacheManager cacheManager,
        Action<NotificationData> callback)
    {
        // Register as NOT_IN_FOCUS - app not yet loaded
        cacheManager.Subscribe(appId, "Volume1", null, FocusLevel.NotInFocus, callback);
        cacheManager.Subscribe(appId, "Volume2", VolumeAspect.Tissue, FocusLevel.NotInFocus, callback);
    }
    
    public async Task PreLoadDataAsync(string appId, IPrivateCache privateCache)
    {
        // Load initial data from external source
        var tissueData = await _dataSource.GetTissueDataAsync();
        
        // Convert to app-specific format and store
        var appData = _converter.ToAppFormat(appId, tissueData);
        privateCache.Store("Volume1", VolumeAspect.Tissue, appData);
    }
    
    private void OnDataChanged(NotificationData notification)
    {
        // Update pre-loaded caches for all target apps
        foreach (var appId in TargetApplications)
        {
            var cache = _cacheRegistry.GetPreLoadedCache(appId);
            if (cache != null)
            {
                var appData = _converter.ToAppFormat(appId, notification.Change);
                cache.Store(notification.VolumeId, notification.Aspect, appData);
            }
        }
    }
}
```

---

## 4. Data Types

### 4.1 Enums

```csharp
/// <summary>
/// Defines the aspects/sub-domains of a volume.
/// Framework-defined; all applications use the same enum.
/// </summary>
public enum VolumeAspect
{
    Tissue,
    AnatomicalPath,
    TBD
}

/// <summary>
/// The type of change that occurred to data.
/// </summary>
public enum ChangeType
{
    Created,
    Updated,
    Deleted
}

/// <summary>
/// The subscription focus level.
/// </summary>
public enum FocusLevel
{
    /// <summary>Receives real-time push notifications.</summary>
    InFocus,
    
    /// <summary>No push; pulls data on focus switch.</summary>
    NotInFocus
}
```

### 4.2 Subscription Key

```csharp
/// <summary>
/// Composite key for subscription lookups.
/// </summary>
public readonly struct SubscriptionKey : IEquatable<SubscriptionKey>
{
    public string VolumeId { get; }
    public VolumeAspect? Aspect { get; }
    
    public SubscriptionKey(string volumeId, VolumeAspect? aspect = null)
    {
        VolumeId = volumeId ?? throw new ArgumentNullException(nameof(volumeId));
        Aspect = aspect;
    }
    
    public bool Equals(SubscriptionKey other) => 
        VolumeId == other.VolumeId && Aspect == other.Aspect;
    
    public override bool Equals(object? obj) => 
        obj is SubscriptionKey other && Equals(other);
    
    public override int GetHashCode() => HashCode.Combine(VolumeId, Aspect);
    
    public override string ToString() => 
        Aspect.HasValue ? $"{VolumeId}:{Aspect}" : VolumeId;
}
```

### 4.3 Subscription Entry

```csharp
/// <summary>
/// Represents a single subscription in the registry.
/// </summary>
public class SubscriptionEntry
{
    public string AppId { get; }
    public string VolumeId { get; }
    public VolumeAspect? Aspect { get; }
    public FocusLevel FocusLevel { get; set; }
    public Action<NotificationData> Callback { get; }
    public DateTime SubscribedAt { get; }
    public DateTime LastSync { get; set; }
    
    public SubscriptionKey Key => new(VolumeId, Aspect);
    
    public SubscriptionEntry(
        string appId,
        string volumeId,
        VolumeAspect? aspect,
        FocusLevel focusLevel,
        Action<NotificationData> callback)
    {
        AppId = appId ?? throw new ArgumentNullException(nameof(appId));
        VolumeId = volumeId ?? throw new ArgumentNullException(nameof(volumeId));
        Aspect = aspect;
        FocusLevel = focusLevel;
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        SubscribedAt = DateTime.UtcNow;
        LastSync = DateTime.UtcNow;
    }
}
```

### 4.4 Notification Data

```csharp
/// <summary>
/// Data delivered to subscribers on notifications.
/// </summary>
public class NotificationData
{
    public string VolumeId { get; }
    public IVolumeAspectChange Change { get; }
    public string TriggeredBy { get; }
    public DateTime Timestamp { get; }
    
    // Convenience properties
    public VolumeAspect Aspect => Change.Aspect;
    public ChangeType ChangeType => Change.ChangeType;
    
    public NotificationData(
        string volumeId,
        IVolumeAspectChange change,
        string triggeredBy,
        DateTime timestamp)
    {
        VolumeId = volumeId;
        Change = change;
        TriggeredBy = triggeredBy;
        Timestamp = timestamp;
    }
}
```

### 4.5 Change Interfaces and Classes

```csharp
/// <summary>
/// Base interface for all volume aspect changes.
/// </summary>
public interface IVolumeAspectChange
{
    VolumeAspect Aspect { get; }
    ChangeType ChangeType { get; }
}

// See class-diagrams.md for full hierarchy:
// - TissueData, TissueChange (abstract), TissueCreated, TissueUpdated, TissueDeleted
// - AnatomicalPathData, AnatomicalPathChange (abstract), etc.
```

---

## 5. Error Handling

### 5.1 Exceptions

| Exception | When Thrown | Handling |
|-----------|-------------|----------|
| `ArgumentNullException` | Null required parameter | Fail immediately |
| `ArgumentException` | Empty/invalid string | Fail immediately |
| `InvalidOperationException` | Operation on non-existent subscription | Fail immediately |

### 5.2 Callback Errors

- **CommonVolumeCache Callbacks**: Logged, not rethrown. Other subscribers not affected.
- **Conversion failures**: Logged with full context. VolumeCache not updated.

---

## 6. Thread Safety Guarantees

| Operation | Guarantee |
|-----------|-----------|
| Subscribe | Thread-safe (ConcurrentDictionary + lock on list) |
| Unsubscribe | Thread-safe |
| UnsubscribeAll | Thread-safe (removes all atomically per app) |
| ChangeFocus | Thread-safe, atomic |
| ChangeFocusAll | Thread-safe (each subscription atomic) |
| Write | Thread-safe |
| Read | Thread-safe |
| Notification delivery | Independent per subscriber |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-03-23 | | Initial API contracts |
| 1.1 | 2026-03-23 | | Added UnsubscribeAll, ChangeFocusAll methods; Updated SyncResult with VolumeId || 1.2 | 2026-03-23 | | Added IGenerator interface for background pre-loading |
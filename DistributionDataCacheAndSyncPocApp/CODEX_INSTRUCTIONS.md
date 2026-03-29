# Codex Instructions: Distribution Data Cache and Sync POC

> **Purpose**: Complete instructions for OpenAI Codex to implement the Distribution Data Cache and Sync Framework POC with a **visual WPF demonstration** showing real-time data synchronization across multiple applications.

---

## 📋 Project Overview

### What You're Building

A C# / .NET in-memory cache and subscription framework that enables multiple applications within a single process to share data efficiently. The POC includes a **visual WPF demo** with 3 application panels that demonstrate cache synchronization in real-time using **shapes and colors**.

**Framework Features:**
1. **CommonVolumeCache** - A shared cache storing data in a common format
2. **Subscription Registry** - Tracks which apps subscribe to which data topics
3. **Notification System** - Async fire-and-forget push notifications to "In Focus" subscribers
4. **Focus Management** - Two-tier subscription (IN_FOCUS = push, NOT_IN_FOCUS = pull on switch)

---

## 🎨 Visual Demo Concept

### Domain Mapping: Shapes = Volumes, Colors = Aspects

The demo uses **shapes** to represent **Volumes** and **colors** to represent **Aspects** (the data that can change):

| Shape | Volume ID | Description |
|-------|-----------|-------------|
| ■ **Square** | `Square` | Volume 1 - First data object |
| ● **Circle** | `Circle` | Volume 2 - Second data object |
| ▲ **Triangle** | `Triangle` | Volume 3 - Third data object |

| Color | Aspect | Hex Code |
|-------|--------|----------|
| 🟡 **Yellow** | `Color` aspect = Yellow | `#FFD700` |
| 🔵 **Blue** | `Color` aspect = Blue | `#1E90FF` |
| 🔴 **Red** | `Color` aspect = Red | `#FF4444` |

### Application Layout (3 Panels in One Window)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      🔄 Shape Sync Demo - Cache POC                         │
├───────────────────────┬───────────────────────┬─────────────────────────────┤
│     APPLICATION 1     │     APPLICATION 2     │       APPLICATION 3         │
│     ─────────────     │     ─────────────     │       ─────────────         │
│                       │                       │                             │
│  Focus: [ Square ▼ ]  │  Focus: [ Square ▼ ]  │  Focus: [ Circle ▼ ]        │
│                       │                       │                             │
│  ┌─────────────────┐  │  ┌─────────────────┐  │  ┌─────────────────┐        │
│  │                 │  │  │                 │  │  │                 │        │
│  │   ■ (Yellow)    │  │  │   ■ (Yellow)    │  │  │   ● (Blue)      │        │
│  │                 │  │  │                 │  │  │                 │        │
│  └─────────────────┘  │  └─────────────────┘  │  └─────────────────┘        │
│                       │                       │                             │
│  Change Color:        │  Change Color:        │  Change Color:              │
│  [🟡 Yellow] [🔵 Blue]│  [🟡 Yellow] [🔵 Blue]│  [🟡 Yellow] [🔵 Blue]      │
│  [🔴 Red]             │  [🔴 Red]             │  [🔴 Red]                   │
│                       │                       │                             │
│  Status: IN_FOCUS     │  Status: IN_FOCUS     │  Status: IN_FOCUS           │
│  Last Update: 10:32:05│  Last Update: 10:32:05│  Last Update: 10:31:42      │
│                       │                       │                             │
│  Can View:            │  Can View:            │  Can View:                  │
│  ■ Square, ● Circle   │  ■ Square, ● Circle   │  ● Circle, ▲ Triangle       │
└───────────────────────┴───────────────────────┴─────────────────────────────┘
│                              Event Log                                      │
│  [10:32:05] App1 changed Square color to Blue                               │
│  [10:32:05] App2 received notification: Square → Blue                       │
│  [10:32:08] App3 switched focus to Triangle                                 │
│  [10:32:08] App3 synced Triangle (was stale)                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Application Capabilities

| App | Can Present | Notes |
|-----|-------------|-------|
| **App 1** | ■ Square, ● Circle | Shares Square with App2, Circle with App2 & App3 |
| **App 2** | ■ Square, ● Circle | Same as App1 |
| **App 3** | ● Circle, ▲ Triangle | Unique access to Triangle, shares Circle with App1 & App2 |

### Demo Scenarios to Demonstrate

#### Scenario 1: Real-time Sync (IN_FOCUS Push)
1. App1 and App2 both focus on Square
2. App1 clicks "Blue" button → Square turns Blue
3. **Result**: App2's Square **instantly** turns Blue (IN_FOCUS push notification)
4. **Visual**: Both panels show blue square simultaneously

#### Scenario 2: Focus Switch Sync (Pull on Switch)
1. App1 focuses on Square, App2 focuses on Circle
2. App1 changes Square to Red (App2 doesn't see it - different focus)
3. App2 switches focus dropdown to Square
4. **Result**: App2 pulls latest data, sees Red Square
5. **Visual**: App2 shows red square after dropdown change

#### Scenario 3: Isolated Changes (No Cross-Volume Sync)
1. App3 focuses on Triangle (only App3 can see Triangle)
2. App3 changes Triangle to Yellow
3. **Result**: App1 and App2 see NO change (they can't view Triangle)
4. **Visual**: Only App3's panel shows yellow triangle

#### Scenario 4: No Self-Notification
1. App1 changes Square to Blue
2. **Result**: App1 does NOT receive a notification (triggering app excluded)
3. **Visual**: Event log shows "App1 changed..." but NOT "App1 received..."

---

## 🛠️ Technology Stack

| Layer | Technology |
|-------|------------|
| Language | C# / .NET 8 |
| UI Framework | **WPF** (Windows Presentation Foundation) |
| Cache | In-memory (`ConcurrentDictionary`) |
| Subscriptions | Registry pattern with `Action<T>` delegates |
| Threading | `Task.Run()` for async notifications, `Dispatcher` for UI updates |
| Concurrency | `ConcurrentDictionary`, `lock` where needed |
| Logging | `Microsoft.Extensions.Logging` |
| DI | `Microsoft.Extensions.DependencyInjection` |

---

## 📁 Project Structure

```
DistributionDataCacheAndSync-poc/
├── src/
│   ├── CacheFramework/
│   │   ├── CacheFramework.csproj
│   │   ├── Interfaces/
│   │   │   ├── ICacheManager.cs
│   │   │   ├── ISubscriptionRegistry.cs
│   │   │   ├── ICachePublisher.cs
│   │   │   ├── ITimestampManager.cs
│   │   │   ├── ICacheStorage.cs
│   │   │   └── IShapeChange.cs
│   │   ├── Models/
│   │   │   ├── Enums/
│   │   │   │   ├── ShapeType.cs
│   │   │   │   ├── ShapeColor.cs
│   │   │   │   ├── ChangeType.cs
│   │   │   │   └── FocusLevel.cs
│   │   │   ├── CacheKey.cs
│   │   │   ├── SubscriptionEntry.cs
│   │   │   ├── SubscriptionInfo.cs
│   │   │   ├── NotificationData.cs
│   │   │   ├── SyncResult.cs
│   │   │   └── Changes/
│   │   │       ├── ShapeData.cs
│   │   │       ├── ShapeColorChange.cs
│   │   │       └── ShapeColorUpdated.cs
│   │   └── Services/
│   │       ├── CacheManager.cs
│   │       ├── SubscriptionRegistry.cs
│   │       ├── CachePublisher.cs
│   │       ├── TimestampManager.cs
│   │       └── CacheStorage.cs
│   │
│   └── ShapeSyncDemo/
│       ├── ShapeSyncDemo.csproj
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── MainWindow.xaml
│       ├── MainWindow.xaml.cs
│       ├── Controls/
│       │   ├── AppPanel.xaml
│       │   └── AppPanel.xaml.cs
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   └── AppPanelViewModel.cs
│       ├── Converters/
│       │   └── ShapeColorToBrushConverter.cs
│       └── Services/
│           └── UIDispatcher.cs
│
├── tests/
│   └── CacheFramework.Tests/
│       ├── CacheFramework.Tests.csproj
│       ├── SubscriptionRegistryTests.cs
│       ├── CacheManagerTests.cs
│       └── IntegrationTests/
│           └── SyncFlowTests.cs
│
└── ShapeSyncDemo.sln
```

---

## 🔧 Implementation Instructions

### Part 1: Cache Framework (CacheFramework Project)

#### Step 1: Create Enums

##### ShapeType.cs (Represents Volumes)
```csharp
namespace CacheFramework.Models.Enums;

/// <summary>
/// Shape types representing different volumes (data objects).
/// </summary>
public enum ShapeType
{
    Square,    // Volume 1
    Circle,    // Volume 2
    Triangle   // Volume 3
}
```

##### ShapeColor.cs (Represents the Aspect Value)
```csharp
namespace CacheFramework.Models.Enums;

/// <summary>
/// Colors that can be applied to shapes.
/// This is the data that gets synchronized.
/// </summary>
public enum ShapeColor
{
    Yellow,  // #FFD700
    Blue,    // #1E90FF
    Red      // #FF4444
}

public static class ShapeColorExtensions
{
    public static string ToHex(this ShapeColor color) => color switch
    {
        ShapeColor.Yellow => "#FFD700",
        ShapeColor.Blue => "#1E90FF",
        ShapeColor.Red => "#FF4444",
        _ => "#FFFFFF"
    };
}
```

##### ChangeType.cs
```csharp
namespace CacheFramework.Models.Enums;

public enum ChangeType
{
    Created,
    Updated,
    Deleted
}
```

##### FocusLevel.cs
```csharp
namespace CacheFramework.Models.Enums;

public enum FocusLevel
{
    /// <summary>Receives real-time push notifications</summary>
    InFocus,
    
    /// <summary>No push; pulls data when switching to focus</summary>
    NotInFocus
}
```

#### Step 2: Create Core Models

##### CacheKey.cs
```csharp
namespace CacheFramework.Models;

using CacheFramework.Models.Enums;

/// <summary>
/// Composite key for cache lookups. ShapeType is the "volume".
/// </summary>
public readonly struct CacheKey : IEquatable<CacheKey>
{
    public ShapeType Shape { get; }
    
    public CacheKey(ShapeType shape)
    {
        Shape = shape;
    }
    
    public bool Equals(CacheKey other) => Shape == other.Shape;
    public override bool Equals(object? obj) => obj is CacheKey key && Equals(key);
    public override int GetHashCode() => Shape.GetHashCode();
    public override string ToString() => Shape.ToString();
}
```

##### ShapeData.cs
```csharp
namespace CacheFramework.Models.Changes;

using CacheFramework.Models.Enums;

/// <summary>
/// The data stored in cache for each shape.
/// </summary>
public class ShapeData
{
    public required ShapeType Shape { get; init; }
    public required ShapeColor Color { get; init; }
    public DateTime LastModified { get; init; } = DateTime.UtcNow;
}
```

##### IShapeChange.cs
```csharp
namespace CacheFramework.Interfaces;

using CacheFramework.Models.Enums;

/// <summary>
/// Base interface for shape changes.
/// </summary>
public interface IShapeChange
{
    ShapeType Shape { get; }
    ChangeType ChangeType { get; }
}
```

##### ShapeColorUpdated.cs
```csharp
namespace CacheFramework.Models.Changes;

using CacheFramework.Interfaces;
using CacheFramework.Models.Enums;

/// <summary>
/// Represents a color change to a shape.
/// </summary>
public class ShapeColorUpdated : IShapeChange
{
    public ShapeType Shape { get; init; }
    public ChangeType ChangeType => ChangeType.Updated;
    public required ShapeColor NewColor { get; init; }
}
```

##### SubscriptionEntry.cs
```csharp
namespace CacheFramework.Models;

using CacheFramework.Models.Enums;

/// <summary>
/// Represents a subscription by an application to a shape.
/// </summary>
public class SubscriptionEntry
{
    public string AppId { get; }
    public ShapeType Shape { get; }
    public FocusLevel FocusLevel { get; set; }
    public Action<NotificationData> Callback { get; }
    public DateTime SubscribedAt { get; }
    public DateTime LastSync { get; set; }
    
    public SubscriptionEntry(
        string appId,
        ShapeType shape,
        FocusLevel focusLevel,
        Action<NotificationData> callback)
    {
        AppId = appId ?? throw new ArgumentNullException(nameof(appId));
        Shape = shape;
        FocusLevel = focusLevel;
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        SubscribedAt = DateTime.UtcNow;
        LastSync = DateTime.UtcNow;
    }
}
```

##### NotificationData.cs
```csharp
namespace CacheFramework.Models;

using CacheFramework.Interfaces;
using CacheFramework.Models.Enums;

/// <summary>
/// Data delivered to subscribers when a change occurs.
/// </summary>
public class NotificationData
{
    public ShapeType Shape { get; }
    public IShapeChange Change { get; }
    public string TriggeredBy { get; }
    public DateTime Timestamp { get; }
    
    public NotificationData(ShapeType shape, IShapeChange change, string triggeredBy, DateTime timestamp)
    {
        Shape = shape;
        Change = change;
        TriggeredBy = triggeredBy;
        Timestamp = timestamp;
    }
}
```

##### SyncResult.cs
```csharp
namespace CacheFramework.Models;

using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;

/// <summary>
/// Returned when a focus switch requires syncing data.
/// </summary>
public class SyncResult
{
    public required ShapeType Shape { get; init; }
    public required ShapeData Data { get; init; }
    public required DateTime Timestamp { get; init; }
}
```

#### Step 3: Create Interfaces

##### ICacheManager.cs
```csharp
namespace CacheFramework.Interfaces;

using CacheFramework.Models;
using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;

/// <summary>
/// Primary interface for applications to interact with the cache.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Subscribe to a shape with specified focus level.
    /// </summary>
    void Subscribe(string appId, ShapeType shape, FocusLevel focus, Action<NotificationData> callback);
    
    /// <summary>
    /// Unsubscribe from a shape.
    /// </summary>
    void Unsubscribe(string appId, ShapeType shape);
    
    /// <summary>
    /// Change focus level. Returns SyncResult if data is stale when switching to InFocus.
    /// </summary>
    SyncResult? ChangeFocus(string appId, ShapeType shape, FocusLevel newFocus);
    
    /// <summary>
    /// Write a change to the cache. Notifies IN_FOCUS subscribers (excluding triggering app).
    /// </summary>
    void Write(string appId, ShapeType shape, IShapeChange change);
    
    /// <summary>
    /// Read current data for a shape.
    /// </summary>
    ShapeData? Read(ShapeType shape);
    
    /// <summary>
    /// Get last modified timestamp.
    /// </summary>
    DateTime? GetLastModified(ShapeType shape);
}
```

##### ISubscriptionRegistry.cs
```csharp
namespace CacheFramework.Interfaces;

using CacheFramework.Models;
using CacheFramework.Models.Enums;

public interface ISubscriptionRegistry
{
    void Add(SubscriptionEntry entry);
    void Remove(string appId, ShapeType shape);
    void UpdateFocus(string appId, ShapeType shape, FocusLevel newFocus);
    SubscriptionEntry? Get(string appId, ShapeType shape);
    IEnumerable<SubscriptionEntry> GetByShape(ShapeType shape, FocusLevel? focus = null);
    IEnumerable<SubscriptionEntry> GetByApp(string appId);
}
```

##### ICachePublisher.cs
```csharp
namespace CacheFramework.Interfaces;

using CacheFramework.Models;

public interface ICachePublisher
{
    /// <summary>
    /// Notify subscribers asynchronously (fire-and-forget).
    /// Excludes the triggering app.
    /// </summary>
    void NotifyAsync(IEnumerable<SubscriptionEntry> subscribers, NotificationData data, string excludeAppId);
}
```

##### ITimestampManager.cs
```csharp
namespace CacheFramework.Interfaces;

using CacheFramework.Models.Enums;

public interface ITimestampManager
{
    void Update(ShapeType shape);
    DateTime? Get(ShapeType shape);
}
```

##### ICacheStorage.cs
```csharp
namespace CacheFramework.Interfaces;

using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;

public interface ICacheStorage
{
    void Store(ShapeType shape, ShapeData data);
    ShapeData? Get(ShapeType shape);
}
```

#### Step 4: Implement Services

##### SubscriptionRegistry.cs
```csharp
namespace CacheFramework.Services;

using System.Collections.Concurrent;
using CacheFramework.Interfaces;
using CacheFramework.Models;
using CacheFramework.Models.Enums;

public class SubscriptionRegistry : ISubscriptionRegistry
{
    private readonly ConcurrentDictionary<(string AppId, ShapeType Shape), SubscriptionEntry> _subscriptions = new();
    private readonly object _lock = new();
    
    public void Add(SubscriptionEntry entry)
    {
        var key = (entry.AppId, entry.Shape);
        _subscriptions.AddOrUpdate(key, entry, (_, existing) =>
        {
            existing.FocusLevel = entry.FocusLevel;
            return existing;
        });
    }
    
    public void Remove(string appId, ShapeType shape)
    {
        _subscriptions.TryRemove((appId, shape), out _);
    }
    
    public void UpdateFocus(string appId, ShapeType shape, FocusLevel newFocus)
    {
        if (_subscriptions.TryGetValue((appId, shape), out var entry))
        {
            entry.FocusLevel = newFocus;
        }
    }
    
    public SubscriptionEntry? Get(string appId, ShapeType shape)
    {
        return _subscriptions.TryGetValue((appId, shape), out var entry) ? entry : null;
    }
    
    public IEnumerable<SubscriptionEntry> GetByShape(ShapeType shape, FocusLevel? focus = null)
    {
        var result = _subscriptions.Values.Where(e => e.Shape == shape);
        if (focus.HasValue)
            result = result.Where(e => e.FocusLevel == focus.Value);
        return result.ToList();
    }
    
    public IEnumerable<SubscriptionEntry> GetByApp(string appId)
    {
        return _subscriptions.Values.Where(e => e.AppId == appId).ToList();
    }
}
```

##### CachePublisher.cs
```csharp
namespace CacheFramework.Services;

using CacheFramework.Interfaces;
using CacheFramework.Models;
using Microsoft.Extensions.Logging;

public class CachePublisher : ICachePublisher
{
    private readonly ILogger<CachePublisher> _logger;
    
    public CachePublisher(ILogger<CachePublisher> logger)
    {
        _logger = logger;
    }
    
    public void NotifyAsync(IEnumerable<SubscriptionEntry> subscribers, NotificationData data, string excludeAppId)
    {
        foreach (var subscriber in subscribers)
        {
            // Skip the triggering app (no self-notification per ADR-0001)
            if (subscriber.AppId == excludeAppId)
                continue;
            
            // Fire-and-forget: each subscriber gets independent task
            _ = Task.Run(() =>
            {
                try
                {
                    subscriber.Callback(data);
                    _logger.LogDebug("Notified {AppId} for {Shape}", subscriber.AppId, data.Shape);
                }
                catch (Exception ex)
                {
                    // Log but don't fail others
                    _logger.LogError(ex, "Failed to notify {AppId} for {Shape}", subscriber.AppId, data.Shape);
                }
            });
        }
    }
}
```

##### TimestampManager.cs
```csharp
namespace CacheFramework.Services;

using System.Collections.Concurrent;
using CacheFramework.Interfaces;
using CacheFramework.Models.Enums;

public class TimestampManager : ITimestampManager
{
    private readonly ConcurrentDictionary<ShapeType, DateTime> _timestamps = new();
    
    public void Update(ShapeType shape)
    {
        _timestamps[shape] = DateTime.UtcNow;
    }
    
    public DateTime? Get(ShapeType shape)
    {
        return _timestamps.TryGetValue(shape, out var ts) ? ts : null;
    }
}
```

##### CacheStorage.cs
```csharp
namespace CacheFramework.Services;

using System.Collections.Concurrent;
using CacheFramework.Interfaces;
using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;

public class CacheStorage : ICacheStorage
{
    private readonly ConcurrentDictionary<ShapeType, ShapeData> _data = new();
    
    public void Store(ShapeType shape, ShapeData data)
    {
        _data[shape] = data;
    }
    
    public ShapeData? Get(ShapeType shape)
    {
        return _data.TryGetValue(shape, out var data) ? data : null;
    }
}
```

##### CacheManager.cs (Main Orchestrator)
```csharp
namespace CacheFramework.Services;

using CacheFramework.Interfaces;
using CacheFramework.Models;
using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;
using Microsoft.Extensions.Logging;

public class CacheManager : ICacheManager
{
    private readonly ISubscriptionRegistry _registry;
    private readonly ICachePublisher _publisher;
    private readonly ITimestampManager _timestamps;
    private readonly ICacheStorage _storage;
    private readonly ILogger<CacheManager> _logger;
    
    public CacheManager(
        ISubscriptionRegistry registry,
        ICachePublisher publisher,
        ITimestampManager timestamps,
        ICacheStorage storage,
        ILogger<CacheManager> logger)
    {
        _registry = registry;
        _publisher = publisher;
        _timestamps = timestamps;
        _storage = storage;
        _logger = logger;
    }
    
    public void Subscribe(string appId, ShapeType shape, FocusLevel focus, Action<NotificationData> callback)
    {
        var entry = new SubscriptionEntry(appId, shape, focus, callback);
        _registry.Add(entry);
        _logger.LogInformation("[{AppId}] Subscribed to {Shape} as {Focus}", appId, shape, focus);
    }
    
    public void Unsubscribe(string appId, ShapeType shape)
    {
        _registry.Remove(appId, shape);
        _logger.LogInformation("[{AppId}] Unsubscribed from {Shape}", appId, shape);
    }
    
    public SyncResult? ChangeFocus(string appId, ShapeType shape, FocusLevel newFocus)
    {
        var entry = _registry.Get(appId, shape);
        if (entry == null)
        {
            _logger.LogWarning("[{AppId}] No subscription for {Shape}", appId, shape);
            return null;
        }
        
        var oldFocus = entry.FocusLevel;
        _registry.UpdateFocus(appId, shape, newFocus);
        _logger.LogInformation("[{AppId}] Changed {Shape} focus: {Old} → {New}", appId, shape, oldFocus, newFocus);
        
        // Check if sync needed when switching TO InFocus
        if (newFocus == FocusLevel.InFocus && oldFocus == FocusLevel.NotInFocus)
        {
            var cacheTimestamp = _timestamps.Get(shape);
            if (cacheTimestamp.HasValue && cacheTimestamp.Value > entry.LastSync)
            {
                var data = _storage.Get(shape);
                if (data != null)
                {
                    entry.LastSync = cacheTimestamp.Value;
                    _logger.LogInformation("[{AppId}] Synced stale data for {Shape}", appId, shape);
                    return new SyncResult
                    {
                        Shape = shape,
                        Data = data,
                        Timestamp = cacheTimestamp.Value
                    };
                }
            }
        }
        
        return null;
    }
    
    public void Write(string appId, ShapeType shape, IShapeChange change)
    {
        // 1. Extract and store data
        if (change is ShapeColorUpdated colorUpdate)
        {
            var data = new ShapeData
            {
                Shape = shape,
                Color = colorUpdate.NewColor,
                LastModified = DateTime.UtcNow
            };
            _storage.Store(shape, data);
        }
        
        // 2. Update timestamp
        _timestamps.Update(shape);
        
        // 3. Find IN_FOCUS subscribers
        var subscribers = _registry.GetByShape(shape, FocusLevel.InFocus);
        
        // 4. Create notification
        var notification = new NotificationData(shape, change, appId, DateTime.UtcNow);
        
        // 5. Notify (excluding self)
        _publisher.NotifyAsync(subscribers, notification, appId);
        
        var notifiedCount = subscribers.Count(s => s.AppId != appId);
        _logger.LogInformation("[{AppId}] Changed {Shape}, notified {Count} subscribers", appId, shape, notifiedCount);
    }
    
    public ShapeData? Read(ShapeType shape)
    {
        return _storage.Get(shape);
    }
    
    public DateTime? GetLastModified(ShapeType shape)
    {
        return _timestamps.Get(shape);
    }
}
```

---

### Part 2: WPF Visual Demo (ShapeSyncDemo Project)

#### Step 5: Create WPF Application

##### ShapeSyncDemo.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\CacheFramework\CacheFramework.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
  </ItemGroup>
</Project>
```

##### App.xaml
```xml
<Application x:Class="ShapeSyncDemo.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <SolidColorBrush x:Key="YellowBrush" Color="#FFD700"/>
        <SolidColorBrush x:Key="BlueBrush" Color="#1E90FF"/>
        <SolidColorBrush x:Key="RedBrush" Color="#FF4444"/>
    </Application.Resources>
</Application>
```

##### App.xaml.cs
```csharp
using System.Windows;
using CacheFramework.Interfaces;
using CacheFramework.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ShapeSyncDemo;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder => builder
            .AddDebug()
            .SetMinimumLevel(LogLevel.Debug));
        
        // Cache Framework (Singleton - shared across all "apps")
        services.AddSingleton<ISubscriptionRegistry, SubscriptionRegistry>();
        services.AddSingleton<ICachePublisher, CachePublisher>();
        services.AddSingleton<ITimestampManager, TimestampManager>();
        services.AddSingleton<ICacheStorage, CacheStorage>();
        services.AddSingleton<ICacheManager, CacheManager>();
        
        // ViewModels
        services.AddTransient<MainViewModel>();
        
        Services = services.BuildServiceProvider();
    }
}
```

##### MainWindow.xaml
```xml
<Window x:Class="ShapeSyncDemo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:ShapeSyncDemo.Controls"
        Title="🔄 Shape Sync Demo - Cache POC" 
        Height="700" Width="1200"
        Background="#1E1E1E">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="150"/>
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <Border Grid.Row="0" Background="#333" Padding="10">
            <TextBlock Text="🔄 Shape Sync Demo - Distributed Cache POC" 
                       FontSize="20" FontWeight="Bold" Foreground="White"/>
        </Border>
        
        <!-- Application Panels -->
        <Grid Grid.Row="1" Margin="10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <local:AppPanel x:Name="App1Panel" Grid.Column="0" Margin="5"
                            AppName="Application 1" AppId="App1"/>
            <local:AppPanel x:Name="App2Panel" Grid.Column="1" Margin="5"
                            AppName="Application 2" AppId="App2"/>
            <local:AppPanel x:Name="App3Panel" Grid.Column="2" Margin="5"
                            AppName="Application 3" AppId="App3"/>
        </Grid>
        
        <!-- Event Log -->
        <Border Grid.Row="2" Background="#2D2D2D" Margin="10" CornerRadius="5">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <TextBlock Text="📋 Event Log" Foreground="White" FontWeight="Bold" Margin="10,5"/>
                <ListBox x:Name="EventLogList" Grid.Row="1" 
                         Background="Transparent" BorderThickness="0"
                         Foreground="#00FF00" FontFamily="Consolas"
                         ItemsSource="{Binding EventLog}"/>
            </Grid>
        </Border>
    </Grid>
</Window>
```

##### MainWindow.xaml.cs
```csharp
using System.Collections.ObjectModel;
using System.Windows;
using CacheFramework.Interfaces;
using CacheFramework.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ShapeSyncDemo;

public partial class MainWindow : Window
{
    public ObservableCollection<string> EventLog { get; } = new();
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        var cacheManager = App.Services.GetRequiredService<ICacheManager>();
        
        // Configure App1: Can view Square, Circle
        App1Panel.Initialize(cacheManager, new[] { ShapeType.Square, ShapeType.Circle }, LogEvent);
        
        // Configure App2: Can view Square, Circle
        App2Panel.Initialize(cacheManager, new[] { ShapeType.Square, ShapeType.Circle }, LogEvent);
        
        // Configure App3: Can view Circle, Triangle
        App3Panel.Initialize(cacheManager, new[] { ShapeType.Circle, ShapeType.Triangle }, LogEvent);
    }
    
    private void LogEvent(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Dispatcher.Invoke(() =>
        {
            EventLog.Insert(0, $"[{timestamp}] {message}");
            if (EventLog.Count > 50) EventLog.RemoveAt(EventLog.Count - 1);
        });
    }
}
```

##### Controls/AppPanel.xaml
```xml
<UserControl x:Class="ShapeSyncDemo.Controls.AppPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Border Background="#3C3C3C" CornerRadius="10" Padding="15">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            
            <!-- App Name -->
            <TextBlock x:Name="AppNameText" Grid.Row="0" 
                       FontSize="16" FontWeight="Bold" Foreground="White"
                       HorizontalAlignment="Center" Margin="0,0,0,10"/>
            
            <!-- Focus Selector -->
            <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,0,0,10">
                <TextBlock Text="Focus:" Foreground="Gray" VerticalAlignment="Center" Margin="0,0,5,0"/>
                <ComboBox x:Name="FocusSelector" Width="120" SelectionChanged="FocusSelector_Changed"/>
            </StackPanel>
            
            <!-- Shape Display -->
            <Border Grid.Row="2" Background="#2D2D2D" CornerRadius="5" Margin="0,0,0,10">
                <Viewbox Stretch="Uniform" Margin="20">
                    <Canvas Width="100" Height="100">
                        <!-- Square -->
                        <Rectangle x:Name="SquareShape" Width="80" Height="80" 
                                   Canvas.Left="10" Canvas.Top="10"
                                   Fill="Gray" Visibility="Collapsed"/>
                        <!-- Circle -->
                        <Ellipse x:Name="CircleShape" Width="80" Height="80"
                                 Canvas.Left="10" Canvas.Top="10"
                                 Fill="Gray" Visibility="Collapsed"/>
                        <!-- Triangle -->
                        <Polygon x:Name="TriangleShape" 
                                 Points="50,10 90,90 10,90"
                                 Fill="Gray" Visibility="Collapsed"/>
                    </Canvas>
                </Viewbox>
            </Border>
            
            <!-- Color Buttons -->
            <TextBlock Grid.Row="3" Text="Change Color:" Foreground="Gray" Margin="0,0,0,5"/>
            <StackPanel Grid.Row="4" Orientation="Horizontal" HorizontalAlignment="Center">
                <Button Content="🟡" Width="50" Height="35" Margin="5" 
                        Click="Yellow_Click" Background="#FFD700"/>
                <Button Content="🔵" Width="50" Height="35" Margin="5" 
                        Click="Blue_Click" Background="#1E90FF"/>
                <Button Content="🔴" Width="50" Height="35" Margin="5" 
                        Click="Red_Click" Background="#FF4444"/>
            </StackPanel>
            
            <!-- Status -->
            <StackPanel Grid.Row="5" Margin="0,10,0,0">
                <TextBlock x:Name="StatusText" Foreground="#00FF00" FontSize="10"/>
                <TextBlock x:Name="CanViewText" Foreground="Gray" FontSize="10"/>
            </StackPanel>
        </Grid>
    </Border>
</UserControl>
```

##### Controls/AppPanel.xaml.cs
```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CacheFramework.Interfaces;
using CacheFramework.Models;
using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;

namespace ShapeSyncDemo.Controls;

public partial class AppPanel : UserControl
{
    public string AppName { get; set; } = "App";
    public string AppId { get; set; } = "App1";
    
    private ICacheManager? _cacheManager;
    private ShapeType[] _allowedShapes = Array.Empty<ShapeType>();
    private ShapeType _currentFocus;
    private Action<string>? _logEvent;
    
    public AppPanel()
    {
        InitializeComponent();
    }
    
    public void Initialize(ICacheManager cacheManager, ShapeType[] allowedShapes, Action<string> logEvent)
    {
        _cacheManager = cacheManager;
        _allowedShapes = allowedShapes;
        _logEvent = logEvent;
        
        AppNameText.Text = AppName;
        CanViewText.Text = $"Can View: {string.Join(", ", allowedShapes.Select(s => GetShapeSymbol(s)))}";
        
        // Populate focus selector
        FocusSelector.ItemsSource = allowedShapes.Select(s => new ComboBoxItem 
        { 
            Content = $"{GetShapeSymbol(s)} {s}",
            Tag = s 
        }).ToList();
        
        // Subscribe to all allowed shapes (NotInFocus initially, except first)
        foreach (var shape in allowedShapes)
        {
            var focus = shape == allowedShapes[0] ? FocusLevel.InFocus : FocusLevel.NotInFocus;
            _cacheManager.Subscribe(AppId, shape, focus, OnNotification);
        }
        
        // Default to first shape
        _currentFocus = allowedShapes[0];
        FocusSelector.SelectedIndex = 0;
        UpdateShapeDisplay();
    }
    
    private void OnNotification(NotificationData notification)
    {
        // Must update UI on dispatcher thread
        Dispatcher.Invoke(() =>
        {
            if (notification.Change is ShapeColorUpdated colorUpdate)
            {
                _logEvent?.Invoke($"[{AppId}] Received: {notification.Shape} → {colorUpdate.NewColor} (from {notification.TriggeredBy})");
                
                // Update display if we're focused on this shape
                if (notification.Shape == _currentFocus)
                {
                    UpdateShapeColor(colorUpdate.NewColor);
                }
            }
        });
    }
    
    private void FocusSelector_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (FocusSelector.SelectedItem is ComboBoxItem item && item.Tag is ShapeType newShape)
        {
            if (newShape == _currentFocus) return;
            
            // Change old focus to NotInFocus
            _cacheManager?.ChangeFocus(AppId, _currentFocus, FocusLevel.NotInFocus);
            
            // Change new focus to InFocus (may return sync data)
            var syncResult = _cacheManager?.ChangeFocus(AppId, newShape, FocusLevel.InFocus);
            
            _currentFocus = newShape;
            
            if (syncResult != null)
            {
                _logEvent?.Invoke($"[{AppId}] Focused on {newShape} - SYNCED (was stale)");
                UpdateShapeColor(syncResult.Data.Color);
            }
            else
            {
                _logEvent?.Invoke($"[{AppId}] Focused on {newShape}");
                // Read current data
                var data = _cacheManager?.Read(newShape);
                if (data != null)
                {
                    UpdateShapeColor(data.Color);
                }
            }
            
            UpdateShapeDisplay();
        }
    }
    
    private void Yellow_Click(object sender, RoutedEventArgs e) => ChangeColor(ShapeColor.Yellow);
    private void Blue_Click(object sender, RoutedEventArgs e) => ChangeColor(ShapeColor.Blue);
    private void Red_Click(object sender, RoutedEventArgs e) => ChangeColor(ShapeColor.Red);
    
    private void ChangeColor(ShapeColor color)
    {
        _cacheManager?.Write(AppId, _currentFocus, new ShapeColorUpdated
        {
            Shape = _currentFocus,
            NewColor = color
        });
        
        // Update own display immediately
        UpdateShapeColor(color);
        _logEvent?.Invoke($"[{AppId}] Changed {_currentFocus} to {color}");
    }
    
    private void UpdateShapeDisplay()
    {
        // Hide all shapes
        SquareShape.Visibility = Visibility.Collapsed;
        CircleShape.Visibility = Visibility.Collapsed;
        TriangleShape.Visibility = Visibility.Collapsed;
        
        // Show current shape
        switch (_currentFocus)
        {
            case ShapeType.Square:
                SquareShape.Visibility = Visibility.Visible;
                break;
            case ShapeType.Circle:
                CircleShape.Visibility = Visibility.Visible;
                break;
            case ShapeType.Triangle:
                TriangleShape.Visibility = Visibility.Visible;
                break;
        }
        
        StatusText.Text = $"Status: IN_FOCUS | {_currentFocus}";
        
        // Load current color from cache
        var data = _cacheManager?.Read(_currentFocus);
        if (data != null)
        {
            UpdateShapeColor(data.Color);
        }
    }
    
    private void UpdateShapeColor(ShapeColor color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color.ToHex()));
        SquareShape.Fill = brush;
        CircleShape.Fill = brush;
        TriangleShape.Fill = brush;
    }
    
    private static string GetShapeSymbol(ShapeType shape) => shape switch
    {
        ShapeType.Square => "■",
        ShapeType.Circle => "●",
        ShapeType.Triangle => "▲",
        _ => "?"
    };
}
```

---

## 🧪 Test Requirements

### Unit Tests

1. **SubscriptionRegistryTests**
   - `Add_NewSubscription_AddsToRegistry`
   - `Add_DuplicateSubscription_UpdatesFocusLevel`
   - `Remove_ExistingSubscription_RemovesFromRegistry`
   - `GetByShape_ReturnsOnlyMatchingFocus`

2. **CacheManagerTests**
   - `Subscribe_RegistersSubscription`
   - `Write_NotifiesInFocusSubscribers`
   - `Write_DoesNotNotifyTriggeringApp` ⚠️ **Critical**
   - `Write_DoesNotNotifyNotInFocus`
   - `ChangeFocus_ToInFocus_SyncsStaleData`
   - `ChangeFocus_ToNotInFocus_DoesNotSync`

3. **Integration Tests**
   - Multi-app write flow with notifications
   - Focus switch with sync verification

---

## ✅ Acceptance Criteria Checklist

### Framework
- [ ] Subscription registry stores and queries subscriptions
- [ ] Write triggers notifications to IN_FOCUS subscribers only
- [ ] Triggering app does NOT receive self-notification
- [ ] Notifications are async fire-and-forget
- [ ] Focus switch to InFocus syncs stale data

### Visual Demo
- [ ] 3 application panels display in main window
- [ ] Each panel shows correct shape based on focus
- [ ] Color buttons change the shape color
- [ ] Real-time sync: Color change in App1 appears instantly in App2 (same focus)
- [ ] Focus switch sync: App2 gets latest data when switching focus
- [ ] Event log shows all operations
- [ ] App3's Triangle changes do NOT affect App1/App2

---

## 🚀 Getting Started

1. Create solution with two projects: `CacheFramework` (class library), `ShapeSyncDemo` (WPF)
2. Implement enums and models
3. Implement interfaces and services
4. Build WPF UI with AppPanel control
5. Test all demo scenarios visually

---

## 📚 Reference Design Documents

| Document | Purpose |
|----------|---------|
| [architecture.md](docs/design/architecture.md) | Full system design |
| [requirements.md](docs/design/requirements.md) | User stories |
| [sequence-diagrams.md](docs/design/diagrams/sequence-diagrams.md) | Interaction flows |
| [ADR-0001](docs/design/adr/0001-async-notification-execution-model.md) | Async notification decision |
| [ADR-0002](docs/design/adr/0002-registry-based-subscription-pattern.md) | Registry vs events decision |

---

**Demo Goal**: When you run the app, you should be able to change a shape's color in one panel and see it instantly update in another panel that's focused on the same shape! 🎉

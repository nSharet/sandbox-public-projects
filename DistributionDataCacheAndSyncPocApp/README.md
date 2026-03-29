# DistributionDataCacheAndSyncPocApp

Proof-of-concept for distribution data cache and synchronization using .NET 8.

## Projects
- `src/CacheFramework`: in-memory cache/sync framework.
- `src/ShapeSyncDemo`: WPF visual demo with 3 simulated applications.
- `tests/CacheFramework.Tests`: unit and integration tests.

## Prerequisites
- Windows machine (WPF desktop app).
- .NET SDK 8.x installed (`dotnet --version`).

## Build and Test
Run from `DistributionDataCacheAndSyncPocApp`:

```powershell
dotnet restore ShapeSyncDemo.sln
dotnet build ShapeSyncDemo.sln -c Debug
dotnet test ShapeSyncDemo.sln -c Debug
```

## Run the Application
From `DistributionDataCacheAndSyncPocApp`, run:

```powershell
dotnet run --project src/ShapeSyncDemo/ShapeSyncDemo.csproj
```

## See It in Action
After the window opens:

1. Real-time sync:
- Set App1 focus to `Square`.
- Set App2 focus to `Square`.
- In App1 click `Blue`.
- App2 should update immediately.

2. Background sync for not-in-focus:
- Set App2 focus to `Circle`.
- In App1 (focused on `Square`) click `Red`.
- Switch App2 focus back to `Square`.
- App2 should show latest `Square` color immediately from its local cache.

3. Isolated shape access:
- In App3 focus `Triangle` and change color.
- App1/App2 should not change because they do not subscribe to `Triangle`.

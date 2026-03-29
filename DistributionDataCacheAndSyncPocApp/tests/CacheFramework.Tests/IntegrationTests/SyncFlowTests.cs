using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;
using CacheFramework.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CacheFramework.Tests.IntegrationTests;

public class SyncFlowTests
{
    [Fact]
    public void MultiAppWriteFlow_NotifiesAllSubscribersExceptWriter()
    {
        var manager = new CacheManager(
            new SubscriptionRegistry(),
            new CachePublisher(NullLogger<CachePublisher>.Instance),
            new TimestampManager(),
            new CacheStorage(),
            NullLogger<CacheManager>.Instance);

        var app2Signal = new ManualResetEventSlim(false);
        var app3Signal = new ManualResetEventSlim(false);

        manager.Subscribe("App1", ShapeType.Square, FocusLevel.InFocus, _ => { });
        manager.Subscribe("App2", ShapeType.Square, FocusLevel.InFocus, _ => app2Signal.Set());
        manager.Subscribe("App3", ShapeType.Square, FocusLevel.NotInFocus, _ => app3Signal.Set());

        manager.Write("App1", ShapeType.Square, new ShapeColorUpdated { Shape = ShapeType.Square, NewColor = ShapeColor.Red });

        Assert.True(app2Signal.Wait(TimeSpan.FromSeconds(1)));
        Assert.True(app3Signal.Wait(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void FocusSwitchFlow_ReturnsLatestDataWhenStale()
    {
        var manager = new CacheManager(
            new SubscriptionRegistry(),
            new CachePublisher(NullLogger<CachePublisher>.Instance),
            new TimestampManager(),
            new CacheStorage(),
            NullLogger<CacheManager>.Instance);

        manager.Subscribe("App1", ShapeType.Circle, FocusLevel.InFocus, _ => { });
        manager.Subscribe("App2", ShapeType.Circle, FocusLevel.NotInFocus, _ => { });

        Thread.Sleep(20);
        manager.Write("App1", ShapeType.Circle, new ShapeColorUpdated { Shape = ShapeType.Circle, NewColor = ShapeColor.Yellow });

        var syncResult = manager.ChangeFocus("App2", ShapeType.Circle, FocusLevel.InFocus);

        Assert.NotNull(syncResult);
        Assert.Equal(ShapeType.Circle, syncResult.Shape);
        Assert.Equal(ShapeColor.Yellow, syncResult.Data.Color);
    }

    [Fact]
    public void TriangleChange_IsolatedFromAppsWithoutTriangleAccess()
    {
        var manager = new CacheManager(
            new SubscriptionRegistry(),
            new CachePublisher(NullLogger<CachePublisher>.Instance),
            new TimestampManager(),
            new CacheStorage(),
            NullLogger<CacheManager>.Instance);

        var app1Signal = new ManualResetEventSlim(false);
        var app2Signal = new ManualResetEventSlim(false);

        manager.Subscribe("App1", ShapeType.Square, FocusLevel.InFocus, _ => app1Signal.Set());
        manager.Subscribe("App2", ShapeType.Circle, FocusLevel.InFocus, _ => app2Signal.Set());
        manager.Subscribe("App3", ShapeType.Triangle, FocusLevel.InFocus, _ => { });

        manager.Write("App3", ShapeType.Triangle, new ShapeColorUpdated { Shape = ShapeType.Triangle, NewColor = ShapeColor.Blue });

        Assert.False(app1Signal.Wait(TimeSpan.FromMilliseconds(250)));
        Assert.False(app2Signal.Wait(TimeSpan.FromMilliseconds(250)));
        Assert.Equal(ShapeColor.Blue, manager.Read(ShapeType.Triangle)?.Color);
    }
}

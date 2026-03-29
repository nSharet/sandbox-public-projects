using CacheFramework.Interfaces;
using CacheFramework.Models;
using CacheFramework.Models.Changes;
using CacheFramework.Models.Enums;
using CacheFramework.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CacheFramework.Tests;

public class CacheManagerTests
{
    [Fact]
    public void Subscribe_RegistersSubscription()
    {
        var registry = new SubscriptionRegistry();
        var sut = CreateManager(registry: registry);

        sut.Subscribe("App1", ShapeType.Square, FocusLevel.InFocus, _ => { });

        Assert.NotNull(registry.Get("App1", ShapeType.Square));
    }

    [Fact]
    public void Write_NotifiesInFocusSubscribers()
    {
        var signal = new ManualResetEventSlim(false);
        var sut = CreateManager();

        sut.Subscribe("App1", ShapeType.Square, FocusLevel.InFocus, _ => { });
        sut.Subscribe("App2", ShapeType.Square, FocusLevel.InFocus, _ => signal.Set());

        sut.Write("App1", ShapeType.Square, new ShapeColorUpdated { Shape = ShapeType.Square, NewColor = ShapeColor.Blue });

        Assert.True(signal.Wait(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Write_DoesNotNotifyTriggeringApp()
    {
        var triggered = false;
        var signal = new ManualResetEventSlim(false);
        var sut = CreateManager();

        sut.Subscribe("App1", ShapeType.Square, FocusLevel.InFocus, _ =>
        {
            triggered = true;
            signal.Set();
        });

        sut.Write("App1", ShapeType.Square, new ShapeColorUpdated { Shape = ShapeType.Square, NewColor = ShapeColor.Red });

        Assert.False(signal.Wait(TimeSpan.FromMilliseconds(250)));
        Assert.False(triggered);
    }

    [Fact]
    public void Write_NotifiesNotInFocusSubscribersForBackgroundSync()
    {
        var signal = new ManualResetEventSlim(false);
        var sut = CreateManager();

        sut.Subscribe("App1", ShapeType.Square, FocusLevel.InFocus, _ => { });
        sut.Subscribe("App2", ShapeType.Square, FocusLevel.NotInFocus, _ => signal.Set());

        sut.Write("App1", ShapeType.Square, new ShapeColorUpdated { Shape = ShapeType.Square, NewColor = ShapeColor.Yellow });

        Assert.True(signal.Wait(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void ChangeFocus_ToInFocus_SyncsStaleData()
    {
        var sut = CreateManager();

        sut.Subscribe("App1", ShapeType.Square, FocusLevel.InFocus, _ => { });
        sut.Subscribe("App2", ShapeType.Square, FocusLevel.NotInFocus, _ => { });

        Thread.Sleep(20);
        sut.Write("App1", ShapeType.Square, new ShapeColorUpdated { Shape = ShapeType.Square, NewColor = ShapeColor.Blue });

        var syncResult = sut.ChangeFocus("App2", ShapeType.Square, FocusLevel.InFocus);

        Assert.NotNull(syncResult);
        Assert.Equal(ShapeColor.Blue, syncResult.Data.Color);
    }

    [Fact]
    public void ChangeFocus_ToNotInFocus_DoesNotSync()
    {
        var sut = CreateManager();

        sut.Subscribe("App1", ShapeType.Square, FocusLevel.InFocus, _ => { });
        sut.Subscribe("App2", ShapeType.Square, FocusLevel.InFocus, _ => { });

        sut.Write("App1", ShapeType.Square, new ShapeColorUpdated { Shape = ShapeType.Square, NewColor = ShapeColor.Blue });
        var syncResult = sut.ChangeFocus("App2", ShapeType.Square, FocusLevel.NotInFocus);

        Assert.Null(syncResult);
    }

    private static CacheManager CreateManager(
        ISubscriptionRegistry? registry = null,
        ICachePublisher? publisher = null,
        ITimestampManager? timestamps = null,
        ICacheStorage? storage = null)
    {
        return new CacheManager(
            registry ?? new SubscriptionRegistry(),
            publisher ?? new CachePublisher(NullLogger<CachePublisher>.Instance),
            timestamps ?? new TimestampManager(),
            storage ?? new CacheStorage(),
            NullLogger<CacheManager>.Instance);
    }
}

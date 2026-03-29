using CacheFramework.Models;
using CacheFramework.Models.Enums;
using CacheFramework.Services;

namespace CacheFramework.Tests;

public class SubscriptionRegistryTests
{
    [Fact]
    public void Add_NewSubscription_AddsToRegistry()
    {
        var registry = new SubscriptionRegistry();

        registry.AddOrUpdate(new SubscriptionEntry("App1", ShapeType.Square, FocusLevel.InFocus, _ => { }));

        var entry = registry.Get("App1", ShapeType.Square);

        Assert.NotNull(entry);
        Assert.Equal(FocusLevel.InFocus, entry.FocusLevel);
    }

    [Fact]
    public void Add_DuplicateSubscription_UpdatesFocusLevel()
    {
        var registry = new SubscriptionRegistry();

        registry.AddOrUpdate(new SubscriptionEntry("App1", ShapeType.Square, FocusLevel.NotInFocus, _ => { }));
        registry.AddOrUpdate(new SubscriptionEntry("App1", ShapeType.Square, FocusLevel.InFocus, _ => { }));

        var entry = registry.Get("App1", ShapeType.Square);

        Assert.NotNull(entry);
        Assert.Equal(FocusLevel.InFocus, entry.FocusLevel);
    }

    [Fact]
    public void Remove_ExistingSubscription_RemovesFromRegistry()
    {
        var registry = new SubscriptionRegistry();
        registry.AddOrUpdate(new SubscriptionEntry("App1", ShapeType.Square, FocusLevel.InFocus, _ => { }));

        var removed = registry.Remove("App1", ShapeType.Square);

        Assert.True(removed);
        Assert.Null(registry.Get("App1", ShapeType.Square));
    }

    [Fact]
    public void GetByShape_ReturnsOnlyMatchingFocus()
    {
        var registry = new SubscriptionRegistry();
        registry.AddOrUpdate(new SubscriptionEntry("App1", ShapeType.Square, FocusLevel.InFocus, _ => { }));
        registry.AddOrUpdate(new SubscriptionEntry("App2", ShapeType.Square, FocusLevel.NotInFocus, _ => { }));
        registry.AddOrUpdate(new SubscriptionEntry("App3", ShapeType.Circle, FocusLevel.InFocus, _ => { }));

        var inFocusSquare = registry.GetByShape(ShapeType.Square, FocusLevel.InFocus);

        Assert.Single(inFocusSquare);
        Assert.Equal("App1", inFocusSquare[0].AppId);
    }
}

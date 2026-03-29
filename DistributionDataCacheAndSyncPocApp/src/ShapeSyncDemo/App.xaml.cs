using System.Windows;
using CacheFramework.Interfaces;
using CacheFramework.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShapeSyncDemo.ViewModels;

namespace ShapeSyncDemo;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddLogging(builder => builder
            .AddDebug()
            .SetMinimumLevel(LogLevel.Debug));

        services.AddSingleton<ISubscriptionRegistry, SubscriptionRegistry>();
        services.AddSingleton<ICachePublisher, CachePublisher>();
        services.AddSingleton<ITimestampManager, TimestampManager>();
        services.AddSingleton<ICacheStorage, CacheStorage>();
        services.AddSingleton<ICacheManager, CacheManager>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<AppPanelViewModel>();

        Services = services.BuildServiceProvider();
    }
}

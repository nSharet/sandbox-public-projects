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

        App1Panel.Initialize(cacheManager, new[] { ShapeType.Square, ShapeType.Circle }, LogEvent);
        App2Panel.Initialize(cacheManager, new[] { ShapeType.Square, ShapeType.Circle }, LogEvent);
        App3Panel.Initialize(cacheManager, new[] { ShapeType.Circle, ShapeType.Triangle }, LogEvent);

        LogEvent("Demo initialized");
        LogEvent("App1 can view: Square, Circle");
        LogEvent("App2 can view: Square, Circle");
        LogEvent("App3 can view: Circle, Triangle");
    }

    private void LogEvent(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Dispatcher.Invoke(() =>
        {
            EventLog.Insert(0, $"[{timestamp}] {message}");
            if (EventLog.Count > 50)
            {
                EventLog.RemoveAt(EventLog.Count - 1);
            }
        });
    }
}

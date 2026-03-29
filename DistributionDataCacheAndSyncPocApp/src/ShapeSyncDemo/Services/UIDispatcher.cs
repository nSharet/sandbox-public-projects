using System.Windows;
using System.Windows.Threading;

namespace ShapeSyncDemo.Services;

public static class UIDispatcher
{
    public static void Invoke(Action action)
    {
        Application.Current.Dispatcher.Invoke(action, DispatcherPriority.Normal);
    }
}

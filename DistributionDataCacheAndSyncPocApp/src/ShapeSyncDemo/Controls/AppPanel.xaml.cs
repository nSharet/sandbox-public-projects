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
    private readonly Dictionary<ShapeType, ShapeColor> _localShapeCache = new();
    private ShapeType _currentFocus;
    private ShapeColor _currentColor = ShapeColor.Yellow;
    private DateTime _lastUpdate = DateTime.Now;
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
        CanViewText.Text = $"Can View: {string.Join(", ", allowedShapes.Select(GetShapeLabel))}";

        FocusSelector.ItemsSource = allowedShapes.Select(s => new ComboBoxItem
        {
            Content = GetShapeLabel(s),
            Tag = s
        }).ToList();

        foreach (var shape in allowedShapes)
        {
            var existing = _cacheManager.Read(shape)?.Color ?? ShapeColor.Yellow;
            _localShapeCache[shape] = existing;

            var focus = shape == allowedShapes[0] ? FocusLevel.InFocus : FocusLevel.NotInFocus;
            _cacheManager.Subscribe(AppId, shape, focus, OnNotification);
        }

        _currentFocus = allowedShapes[0];
        FocusSelector.SelectedIndex = 0;
        UpdateShapeDisplay();
    }

    private void OnNotification(NotificationData notification)
    {
        Dispatcher.Invoke(() =>
        {
            if (notification.Change is not ShapeColorUpdated colorUpdate)
            {
                return;
            }

            _localShapeCache[notification.Shape] = colorUpdate.NewColor;
            _logEvent?.Invoke($"{AppId} received update: {notification.Shape} -> {colorUpdate.NewColor} (from {notification.TriggeredBy})");

            if (notification.Shape == _currentFocus)
            {
                _currentColor = colorUpdate.NewColor;
                _lastUpdate = DateTime.Now;
                UpdateShapeColor(colorUpdate.NewColor);
                UpdateStatusTexts();
            }
        });
    }

    private void FocusSelector_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cacheManager is null)
        {
            return;
        }

        if (FocusSelector.SelectedItem is not ComboBoxItem item || item.Tag is not ShapeType newShape)
        {
            return;
        }

        if (newShape == _currentFocus)
        {
            return;
        }

        _cacheManager.ChangeFocus(AppId, _currentFocus, FocusLevel.NotInFocus);
        _cacheManager.ChangeFocus(AppId, newShape, FocusLevel.InFocus);

        _currentFocus = newShape;

        if (_localShapeCache.TryGetValue(newShape, out var cachedColor))
        {
            _currentColor = cachedColor;
            _lastUpdate = DateTime.Now;
            _logEvent?.Invoke($"{AppId} changed focus to {newShape} (rendered from local cache: {cachedColor})");
            UpdateShapeColor(cachedColor);
        }
        else
        {
            var data = _cacheManager.Read(newShape);
            if (data is not null)
            {
                _currentColor = data.Color;
                _localShapeCache[newShape] = data.Color;
                _lastUpdate = DateTime.Now;
                _logEvent?.Invoke($"{AppId} changed focus to {newShape} (cache read)");
                UpdateShapeColor(data.Color);
            }
            else
            {
                _logEvent?.Invoke($"{AppId} changed focus to {newShape}");
            }
        }

        UpdateShapeDisplay();
    }

    private void Yellow_Click(object sender, RoutedEventArgs e) => ChangeColor(ShapeColor.Yellow);

    private void Blue_Click(object sender, RoutedEventArgs e) => ChangeColor(ShapeColor.Blue);

    private void Red_Click(object sender, RoutedEventArgs e) => ChangeColor(ShapeColor.Red);

    private void ChangeColor(ShapeColor color)
    {
        if (_cacheManager is null)
        {
            return;
        }

        _cacheManager.Write(AppId, _currentFocus, new ShapeColorUpdated
        {
            Shape = _currentFocus,
            NewColor = color
        });

        _localShapeCache[_currentFocus] = color;
        _currentColor = color;
        _lastUpdate = DateTime.Now;
        UpdateShapeColor(color);
        UpdateStatusTexts();
        _logEvent?.Invoke($"{AppId} changed {_currentFocus} color to {color}");
    }

    private void UpdateShapeDisplay()
    {
        SquareShape.Visibility = Visibility.Collapsed;
        CircleShape.Visibility = Visibility.Collapsed;
        TriangleShape.Visibility = Visibility.Collapsed;

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

        if (_localShapeCache.TryGetValue(_currentFocus, out var localColor))
        {
            _currentColor = localColor;
            UpdateShapeColor(localColor);
        }

        UpdateStatusTexts();
    }

    private void UpdateStatusTexts()
    {
        StatusText.Text = $"Status: IN_FOCUS ({_currentFocus})";
        CurrentColorText.Text = $"Current Color: {_currentColor}";
        LastUpdateText.Text = $"Last Update: {_lastUpdate:HH:mm:ss}";
    }

    private void UpdateShapeColor(ShapeColor color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color.ToHex()));
        SquareShape.Fill = brush;
        CircleShape.Fill = brush;
        TriangleShape.Fill = brush;
    }

    private static string GetShapeLabel(ShapeType shape) => shape switch
    {
        ShapeType.Square => "Square",
        ShapeType.Circle => "Circle",
        ShapeType.Triangle => "Triangle",
        _ => "Unknown"
    };
}

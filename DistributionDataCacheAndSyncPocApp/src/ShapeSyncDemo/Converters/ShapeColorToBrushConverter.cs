using CacheFramework.Models.Enums;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShapeSyncDemo.Converters;

public class ShapeColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ShapeColor color)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color.ToHex()));
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

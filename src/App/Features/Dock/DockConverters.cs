using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Koy.App.Features.Dock;

public static class DockConverters
{
    public static readonly IValueConverter BoolToVisibilityConverter = new SimpleBoolToVisibilityConverter();
    public static readonly IValueConverter NullToVisibleConverter = new SimpleNullToVisibleConverter();

    private sealed class SimpleBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class SimpleNullToVisibleConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

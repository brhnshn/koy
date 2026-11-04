using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Koy.App.Features.Dock;

public static class DockConverters
{
    public static readonly IValueConverter BoolToVisibilityConverter = new SimpleBoolToVisibilityConverter();
    public static readonly IValueConverter InverseBoolToVisibilityConverter = new InverseBoolToVisibilityConverterImpl();
    public static readonly IValueConverter NullToVisibleConverter = new SimpleNullToVisibleConverter();
    public static readonly IValueConverter NotNullToVisibleConverter = new NotNullToVisibleConverterImpl();
    public static readonly IValueConverter SingleWindowVisibilityConverter = new SingleWindowVisibilityConverterImpl();
    public static readonly IValueConverter MultiWindowVisibilityConverter = new MultiWindowVisibilityConverterImpl();
    public static readonly IValueConverter InverseBoolConverter = new InverseBoolConverterImpl();

    private sealed class InverseBoolConverterImpl : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is not true;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is not true;
    }

    private sealed class SimpleBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class InverseBoolToVisibilityConverterImpl : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is false ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class SimpleNullToVisibleConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class NotNullToVisibleConverterImpl : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is not null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class SingleWindowVisibilityConverterImpl : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is int count && count == 1 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class MultiWindowVisibilityConverterImpl : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is int count && count > 1 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}

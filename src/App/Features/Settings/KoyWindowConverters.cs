using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Koy.App.Features.Settings;

public sealed class PageIndicatorConverter : IValueConverter
{
    public static readonly PageIndicatorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentIndex && parameter is not null && int.TryParse(parameter.ToString(), out var targetIndex))
        {
            return currentIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class PageIndexToVisibilityConverter : IValueConverter
{
    public static readonly PageIndexToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentIndex && parameter is not null && int.TryParse(parameter.ToString(), out var targetIndex))
        {
            return currentIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class ComparisonToBoolConverter : IValueConverter
{
    public static readonly ComparisonToBoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        if (value is double dVal && double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var targetDouble))
        {
            return Math.Abs(dVal - targetDouble) < 0.1;
        }
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter is not null)
        {
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, parameter.ToString()!, true);
            }
            if (targetType == typeof(int) && int.TryParse(parameter.ToString(), out var iVal))
            {
                return iVal;
            }
            if (targetType == typeof(double) && double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dVal))
            {
                return dVal;
            }
            return parameter.ToString()!;
        }
        return Binding.DoNothing;
    }
}


public sealed class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : false;
}

using System.Globalization;
using System.Windows.Data;

namespace Koy.App.Features.ControlCenter;

/// <summary>Boş olmayan metin için true (hata satırını renklendirmek için).</summary>
public sealed class HasTextConverter : IValueConverter
{
    public static HasTextConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string { Length: > 0 };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>bool değerini tersine çevirir (IsUnavailable → IsEnabled).</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public static InverseBoolConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
}

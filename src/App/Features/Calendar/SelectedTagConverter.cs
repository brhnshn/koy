using System.Globalization;
using System.Windows.Data;

namespace Koy.App.Features.Calendar;

/// <summary>Seçili görünüm düğmesinin Tag'i: true ise "Selected", değilse boş (stil bunu tetikleyici olarak okur).</summary>
public sealed class SelectedTagConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "Selected" : null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

using System.Globalization;
using System.Windows.Data;

namespace Koy.App.Features.Calendar;

/// <summary>Seçili görünüm düğmesinin Tag'ini "Selected" yapar; şablon bu metni tetikleyici olarak kullanır.</summary>
public sealed class PillTagConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "Selected" : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

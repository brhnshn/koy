using System.Globalization;
using System.IO;
using System.Windows.Data;
using Koy.Core.Shelf;

namespace Koy.App.Features.Shelf;

/// <summary>Raf kutucuğundaki kısa etiket: dosyada uzantı ("PDF"), klasörde "DIR", metinde "TXT".</summary>
public sealed class ShelfBadgeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ShelfItem item)
            return "";

        return item.Kind switch
        {
            ShelfItemKind.Directory => "DIR",
            ShelfItemKind.Text => "TXT",
            _ => Extension(item.PathOrContent)
        };
    }

    private static string Extension(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        return ext.Length switch
        {
            0 => "DOSYA",
            > 4 => ext[..4],
            _ => ext
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

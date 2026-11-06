using System.Globalization;
using System.Text.Json;

namespace Koy.Core.Weather;

/// <summary>
/// Cihaz konumundan yer adı (ters konumlama, OpenStreetMap Nominatim <c>reverse</c> yanıtı).
/// Yalnız şehir/ilçe düzeyinde ad seçilir; sokak ve mahalle hiç kullanılmaz.
/// </summary>
public static class PlaceNameParser
{
    // Şehir düzeyinden geniş düzeye: ilk dolu olan yer adıdır.
    private static readonly string[] AddressKeys =
        ["city", "town", "village", "municipality", "county", "province", "state"];

    /// <summary>Yanıttaki yer adı; ayrıştırılamazsa ya da ad yoksa null.</summary>
    public static string? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("address", out var address) ||
                address.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var key in AddressKeys)
            {
                if (address.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var name = value.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Sorguda gönderilen koordinat: iki ondalık (~1 km). Şehir adı için yeter, tam konum servise gitmez;
    /// aynı zamanda önbellek anahtarıdır.
    /// </summary>
    public static (double Latitude, double Longitude) Round(double latitude, double longitude) =>
        (Math.Round(latitude, 2, MidpointRounding.AwayFromZero), Math.Round(longitude, 2, MidpointRounding.AwayFromZero));

    /// <summary>Nominatim reverse sorgu adresi (zoom 10: şehir düzeyi).</summary>
    public static string BuildUrl(double latitude, double longitude, string language)
    {
        var (lat, lon) = Round(latitude, longitude);
        var lang = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "tr";
        return "https://nominatim.openstreetmap.org/reverse?format=jsonv2&zoom=10" +
               $"&lat={lat.ToString("0.00", CultureInfo.InvariantCulture)}" +
               $"&lon={lon.ToString("0.00", CultureInfo.InvariantCulture)}" +
               $"&accept-language={lang}";
    }
}

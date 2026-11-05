using System.Net.Http;
using Koy.Core.Weather;
using Koy.Platform.Net;

namespace Koy.Platform.Weather;

/// <summary>Open-Meteo geocoding: şehir adından koordinat. Anahtar gerekmez; yalnız kullanıcı arama yapınca çağrılır.</summary>
public sealed class GeocodingService(HttpClient? httpClient = null) : IGeocodingService
{
    private readonly HttpClient _http = httpClient ?? AppHttp.Client;

    public async Task<IReadOnlyList<GeoPlace>> SearchAsync(string query, string language, CancellationToken cancellationToken = default)
    {
        var name = query?.Trim() ?? "";
        if (name.Length < OpenMeteoParser.MinQueryLength)
            return [];

        try
        {
            var lang = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "tr";
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(name)}&count={OpenMeteoParser.MaxResults}&language={lang}";

            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            return OpenMeteoParser.ParseGeocoding(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch
        {
            // Ağ yok, zaman aşımı ya da iptal: boş sonuç; arayüz "bulunamadı" der, çökmez.
            return [];
        }
    }
}

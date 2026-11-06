using System.Net.Http;
using Koy.Core.Weather;
using Koy.Platform.Net;

namespace Koy.Platform.Weather;

/// <summary>
/// OpenStreetMap Nominatim ile cihaz konumunun yer adı. Anahtar gerekmez; koordinat ~1 km'ye yuvarlanarak gönderilir.
/// Yalnız çentik açılıp cihaz konumu kullanılırken çağrılır; aynı koordinat için oturumda tek istek atılır
/// (Nominatim kullanım koşulu: saniyede en çok bir istek, tanıtıcı User-Agent; AppHttp bunu taşır).
/// </summary>
public sealed class PlaceNameService(HttpClient? httpClient = null) : IPlaceNameService
{
    private readonly HttpClient _http = httpClient ?? AppHttp.Client;
    private readonly object _gate = new();
    private string? _key;
    private Task<string?>? _pending;

    public Task<string?> GetPlaceNameAsync(double latitude, double longitude, string language, CancellationToken cancellationToken = default)
    {
        if (!OpenMeteoParser.IsValidCoordinate(latitude, longitude))
            return Task.FromResult<string?>(null);

        var (lat, lon) = PlaceNameParser.Round(latitude, longitude);
        var key = FormattableString.Invariant($"{lat:0.00},{lon:0.00},{language}");

        lock (_gate)
        {
            // Aynı koordinat için ya bitmiş sonuç ya da sürmekte olan istek paylaşılır. Başarısız (null) sonuç
            // da tutulur: ağ yoksa her çentik açılışında yeniden denenmesin; koordinat değişince yeniden sorulur.
            if (_key == key && _pending is not null)
                return _pending;

            _key = key;
            _pending = FetchAsync(latitude, longitude, language);
            return _pending;
        }
    }

    private async Task<string?> FetchAsync(double latitude, double longitude, string language)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var response = await _http.GetAsync(PlaceNameParser.BuildUrl(latitude, longitude, language), timeout.Token);
            if (!response.IsSuccessStatusCode)
                return null;

            return PlaceNameParser.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
        }
        catch
        {
            // Ağ yok, zaman aşımı: "Konumum" gösterilmeye devam eder.
            return null;
        }
    }
}

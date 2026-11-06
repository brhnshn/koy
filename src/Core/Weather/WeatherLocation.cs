namespace Koy.Core.Weather;

/// <summary>Hava durumunun koordinatı nereden geldi. <see cref="None"/>: konum yok, hava durumu hiç gösterilmez.</summary>
public enum WeatherLocationSource
{
    None,
    Device,
    City
}

/// <summary>Şehir aramasından (Open-Meteo geocoding) dönen bir yer.</summary>
public sealed record GeoPlace(string Name, string? Region, string? Country, double Latitude, double Longitude)
{
    /// <summary>Listede gösterilen ad: "Ankara, Ankara, Türkiye" gibi tekrarları ayıklanmış biçim.</summary>
    public string DisplayName
    {
        get
        {
            var parts = new List<string> { Name };
            if (!string.IsNullOrWhiteSpace(Region) && !string.Equals(Region, Name, StringComparison.OrdinalIgnoreCase))
                parts.Add(Region!);
            if (!string.IsNullOrWhiteSpace(Country))
                parts.Add(Country!);
            return string.Join(", ", parts);
        }
    }
}

public enum LocationStatus
{
    Success,
    /// <summary>Kullanıcı ya da sistem (Windows gizlilik ayarı) konuma izin vermedi.</summary>
    Denied,
    /// <summary>Konum sensörü ya da konum hizmeti yok / kapalı.</summary>
    Unavailable,
    TimedOut,
    Error
}

public sealed record LocationResult(LocationStatus Status, double Latitude = 0, double Longitude = 0)
{
    public bool IsSuccess => Status == LocationStatus.Success;
}

/// <summary>
/// Cihaz konumu sözleşmesi. Tek seferlik, düşük doğruluklu sorgu; izin reddi, zaman aşımı ve hatalar
/// istisna fırlatmaz, durum olarak döner.
/// </summary>
public interface ILocationService
{
    Task<LocationResult> GetLocationAsync(CancellationToken cancellationToken = default);
}

/// <summary>Koordinattan yer adı (ters konumlama). Ağ hatasında null döner, istisna fırlatmaz.</summary>
public interface IPlaceNameService
{
    Task<string?> GetPlaceNameAsync(double latitude, double longitude, string language, CancellationToken cancellationToken = default);
}

/// <summary>Şehir arama sözleşmesi (Open-Meteo geocoding).</summary>
public interface IGeocodingService
{
    /// <summary>Ağ hatasında boş liste döner, istisna fırlatmaz.</summary>
    Task<IReadOnlyList<GeoPlace>> SearchAsync(string query, string language, CancellationToken cancellationToken = default);
}

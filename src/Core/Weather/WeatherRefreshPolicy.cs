namespace Koy.Core.Weather;

/// <summary>
/// "Hava durumunu yenileme sıklığı" ayarını önbellek süresine çevirir. Ayar metni: "30m", "1h", "startup".
/// Önbellek süresi dolmadan panel açılsa da yeni istek atılmaz; "startup" oturum boyunca yalnız ilk açılışta çeker
/// (elle yenile düğmesi yine çalışır).
/// </summary>
public static class WeatherRefreshPolicy
{
    public const string Every30Minutes = "30m";
    public const string Hourly = "1h";
    public const string StartupOnly = "startup";

    public static TimeSpan CacheDuration(string? setting) => setting?.Trim().ToLowerInvariant() switch
    {
        Hourly => TimeSpan.FromHours(1),
        StartupOnly => TimeSpan.MaxValue,
        _ => TimeSpan.FromMinutes(30)
    };
}

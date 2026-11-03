using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Koy.Core.Weather;

namespace Koy.Platform.Weather;

public sealed class WeatherService : IWeatherService
{
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;

    private WeatherInfo? _cachedWeather;
    private double? _cachedLatitude;
    private double? _cachedLongitude;
    private DateTimeOffset _lastFetchTime = DateTimeOffset.MinValue;

    public WeatherInfo? CurrentWeather => _cachedWeather;

    public event EventHandler<WeatherInfo?>? WeatherChanged;

    public WeatherService(HttpClient? httpClient = null, TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient ?? SharedHttpClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<WeatherInfo?> GetCurrentWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var isSameLocation = _cachedLatitude.HasValue &&
                             _cachedLongitude.HasValue &&
                             Math.Abs(_cachedLatitude.Value - latitude) < 0.001 &&
                             Math.Abs(_cachedLongitude.Value - longitude) < 0.001;

        if (_cachedWeather is not null && isSameLocation && now - _lastFetchTime < CacheDuration)
        {
            return _cachedWeather;
        }

        try
        {
            var latStr = latitude.ToString("F4", CultureInfo.InvariantCulture);
            var lonStr = longitude.ToString("F4", CultureInfo.InvariantCulture);
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,weather_code";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return _cachedWeather;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (doc.RootElement.TryGetProperty("current", out var currentElement))
            {
                var temp = currentElement.GetProperty("temperature_2m").GetDouble();
                var code = currentElement.GetProperty("weather_code").GetInt32();
                var description = WeatherCodeMapper.ToDescription(code);
                var iconKind = WeatherCodeMapper.ToIconKind(code);

                var weather = new WeatherInfo(temp, code, description, iconKind, now);
                _cachedWeather = weather;
                _cachedLatitude = latitude;
                _cachedLongitude = longitude;
                _lastFetchTime = now;

                WeatherChanged?.Invoke(this, _cachedWeather);
                return _cachedWeather;
            }
        }
        catch
        {
            // Ağ hatası veya zaman aşımı durumunda önbellekteki değer döner
        }

        return _cachedWeather;
    }

    public async Task RefreshAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        // Önbellek süresini sıfırlayarak taze veri al
        _lastFetchTime = DateTimeOffset.MinValue;
        await GetCurrentWeatherAsync(latitude, longitude, cancellationToken);
    }

    public void Dispose()
    {
    }
}

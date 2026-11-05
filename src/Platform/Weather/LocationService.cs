using Koy.Core.Weather;
using Windows.Devices.Geolocation;

namespace Koy.Platform.Weather;

/// <summary>
/// Cihaz konumu (Windows.Devices.Geolocation). Her çağrıda tek seferlik, düşük doğruluklu sorgu yapılır; sürekli
/// konum izleme yoktur. Ret, zaman aşımı ve hatalar durum olarak döner, istisna fırlatılmaz.
/// </summary>
public sealed class LocationService : ILocationService
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(12);

    // Bir günlük önbellekli konum yeterli: hava durumu için kilometrelik doğruluk gerekmez.
    private static readonly TimeSpan MaximumAge = TimeSpan.FromHours(24);

    public async Task<LocationResult> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // RequestAccessAsync kullanıcıya Windows'un konum iznini ilk çağrıda sorar; ön plandaki bir
            // pencereden, arayüz iş parçacığında çağrılmalı (çağıran bunu sağlar).
            var access = await Geolocator.RequestAccessAsync().AsTask(cancellationToken);
            switch (access)
            {
                case GeolocationAccessStatus.Allowed:
                    break;
                case GeolocationAccessStatus.Denied:
                    return new LocationResult(LocationStatus.Denied);
                default:
                    return new LocationResult(LocationStatus.Unavailable);
            }

            var locator = new Geolocator
            {
                DesiredAccuracy = PositionAccuracy.Default,
                ReportInterval = 0
            };

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(QueryTimeout + TimeSpan.FromSeconds(3));

            var position = await locator.GetGeopositionAsync(MaximumAge, QueryTimeout).AsTask(timeout.Token);
            var point = position.Coordinate.Point.Position;
            return new LocationResult(LocationStatus.Success, point.Latitude, point.Longitude);
        }
        catch (OperationCanceledException)
        {
            return new LocationResult(LocationStatus.TimedOut);
        }
        catch (UnauthorizedAccessException)
        {
            // Gizlilik ayarı sorgu sırasında kapatıldı ya da izin sonradan reddedildi.
            return new LocationResult(LocationStatus.Denied);
        }
        catch (Exception ex) when (ex is TimeoutException || ex.HResult == unchecked((int)0x800705B4))
        {
            // 0x800705B4: WAIT_TIMEOUT, konum bulunamadı.
            return new LocationResult(LocationStatus.TimedOut);
        }
        catch
        {
            return new LocationResult(LocationStatus.Error);
        }
    }
}

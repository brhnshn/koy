namespace Koy.App.Features;

/// <summary>
/// Bir özelliğin (Notch, Dock…) tek giriş noktası. Kapalı özellik başlatılmaz:
/// penceresi oluşturulmaz, servisi ve hook'u kurulmaz, belleğe yüklenmez.
/// </summary>
public interface IFeature
{
    void Start();

    /// <summary>Pencereleri kapatır, servisleri durdurur, hook'ları çözer. Birden fazla çağrılabilir.</summary>
    void Stop();
}

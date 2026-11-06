namespace Koy.Core.Display;

/// <summary>
/// Ekran parlaklığı kontrol sözleşmesi.
/// Dahili ekranlar (WMI) veya harici monitörler (DDC/CI) için.
/// Desteklenmiyorsa IsSupported false döner. Kaynak ekran düzeni değişince <see cref="Refresh"/> ile yeniden belirlenir.
/// </summary>
public interface IBrightnessService : IDisposable
{
    bool IsSupported { get; }
    int Brightness { get; }

    /// <summary>Şu an kullanılan yol (<see cref="BrightnessSourceKind.None"/> ise IsSupported false).</summary>
    BrightnessSourceKind Source { get; }

    /// <summary>Parlaklık Koy dışından değişti (Fn tuşları, Windows ayarları).</summary>
    event EventHandler<int>? BrightnessChanged;

    /// <summary>Kaynak ya da destek durumu değişti (Refresh sonrası, ya da ayar çağrısı başarısız olunca). Arka plan iş parçacığından gelir.</summary>
    event EventHandler? AvailabilityChanged;

    /// <summary>
    /// Çağıranı bloklamaz: değer arka planda uygulanır, art arda gelenler birleşir (yalnız son değer yazılır).
    /// Koy içinden verilen değer için <see cref="BrightnessChanged"/> yükselmez.
    /// </summary>
    void SetBrightness(int brightness);

    /// <summary>Kaynağı ve geçerli parlaklığı yeniden belirler. WMI/DDC sorgusu yapar: UI iş parçacığından çağrılmaz.</summary>
    void Refresh();
}

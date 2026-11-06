namespace Koy.Core.Bluetooth;

/// <summary>
/// Eşleşmiş Bluetooth cihazlarının listesi, bağlantı durumu ve pili. Servis yaşadığı sürece cihaz izleyicisi çalışır
/// (olay tabanlı, yoklama yok); arayüz yalnız ilgili sayfa görünürken servisi alır.
/// </summary>
public interface IBluetoothService : IDisposable
{
    /// <summary>Şu anki cihazlar (ham; birleştirme ve sıralama <see cref="BluetoothList"/> ile).</summary>
    IReadOnlyList<BluetoothDeviceInfo> Devices { get; }

    /// <summary>Başlangıç taraması bitti mi (liste güvenilir).</summary>
    bool IsReady { get; }

    /// <summary>Cihaz eklendi, çıktı ya da özelliği (bağlantı, pil) değişti. Arka plan iş parçacığından gelir.</summary>
    event EventHandler? DevicesChanged;

    /// <summary>Ses cihazına bağlanmayı dener ve sonucu bağlantı durumundan okur.</summary>
    Task<BluetoothActionResult> ConnectAsync(string deviceId);

    /// <summary>Ses cihazının bağlantısını keser ve sonucu bağlantı durumundan okur.</summary>
    Task<BluetoothActionResult> DisconnectAsync(string deviceId);

    /// <summary>Windows'ta Bluetooth ayarlarını açar.</summary>
    void OpenSettings();
}

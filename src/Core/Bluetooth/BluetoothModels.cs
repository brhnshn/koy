namespace Koy.Core.Bluetooth;

public enum BluetoothDeviceKind
{
    Other,
    Headphones,
    /// <summary>Hoparlör ve diğer ses cihazları.</summary>
    Speaker,
    Mouse,
    Keyboard,
    Phone,
    Tablet
}

/// <summary>Eşleşmiş bir Bluetooth cihazı. <paramref name="ContainerId"/> aynı fiziksel cihazın klasik ve LE kayıtlarını birleştirir.</summary>
public sealed record BluetoothDeviceInfo(
    string Id,
    string Name,
    BluetoothDeviceKind Kind,
    bool IsConnected,
    int? BatteryPercent,
    string? ContainerId = null,
    bool IsLowEnergy = false)
{
    /// <summary>Ses cihazı mı (kulaklık, hoparlör): Koy yalnız bunlar için bağlan / bağlantıyı kes sunar.</summary>
    public bool IsAudio => Kind is BluetoothDeviceKind.Headphones or BluetoothDeviceKind.Speaker;
}

/// <summary>Bağlan / kes isteğinin sonucu; "bağlandı" yalnız gerçek bağlantı durumu doğrulayınca söylenir.</summary>
public enum BluetoothActionResult
{
    /// <summary>İstenen duruma ulaşıldığı bağlantı durumundan okundu.</summary>
    Confirmed,
    /// <summary>İstek sürücüye iletildi ama süre içinde istenen duruma ulaşıldığı görülmedi.</summary>
    NotConfirmed,
    /// <summary>Bu cihaz için Koy'dan bağlanma yolu yok (ses uç noktası bulunamadı).</summary>
    Unsupported,
    Failed
}

public enum BluetoothRowAction
{
    Connect,
    Disconnect,
    /// <summary>Ses dışı cihaz (fare, klavye…): Koy bağlayamaz, Windows Bluetooth ayarları açılır.</summary>
    OpenSettings
}

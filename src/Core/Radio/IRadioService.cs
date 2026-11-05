namespace Koy.Core.Radio;

public enum RadioKind
{
    WiFi,
    Bluetooth
}

public enum RadioState
{
    /// <summary>Radyolar henüz taranmadı.</summary>
    Unknown,
    On,
    Off,
    /// <summary>Donanım anahtarı ya da sürücü radyoyu kapattı; yazılımdan açılamaz.</summary>
    Disabled,
    /// <summary>Tarandı, bu türde radyo yok (adaptör takılı değil ya da erişim yok).</summary>
    NotPresent
}

/// <summary>
/// Windows'un "Uygulamaların radyoları denetlemesine izin ver" gizlilik iznine karşılık gelir.
/// </summary>
public enum RadioAccess
{
    Unknown,
    Allowed,
    DeniedByUser,
    DeniedBySystem
}

/// <summary>
/// Wi-Fi ve Bluetooth radyo durumu ve açma/kapatma sözleşmesi.
/// </summary>
public interface IRadioService : IDisposable
{
    RadioAccess Access { get; }
    RadioState WiFiState { get; }
    RadioState BluetoothState { get; }

    event EventHandler<RadioStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// İzni yeniden sorar, radyoları yeniden bulur ve her tür için <see cref="StateChanged"/> yayınlar.
    /// USB adaptör takılıp çıkarıldığında ya da izin sonradan verildiğinde güncel durumu getirir.
    /// Aynı anda gelen çağrılar tek taramayı paylaşır.
    /// </summary>
    Task RefreshAsync();

    Task<bool> SetStateAsync(RadioKind kind, bool enable);
    Task<bool> ToggleAsync(RadioKind kind);

    /// <summary>Windows'ta ilgili radyonun ayar sayfasını açar (ağ listesi, cihaz eşleştirme orada).</summary>
    void OpenSettings(RadioKind kind);

    /// <summary>Windows'ta radyo denetimi gizlilik sayfasını açar.</summary>
    void OpenPrivacySettings();
}

public sealed record RadioStateChangedEventArgs(RadioKind Kind, RadioState State);

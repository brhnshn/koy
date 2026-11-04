using Koy.Core.Network;

namespace Koy.Core.Dock;

/// <summary>
/// Dock durum alanındaki simgeler için Segoe Fluent Icons karakterleri. Windows 11 görev çubuğuyla aynı yazı tipi.
/// </summary>
public static class TrayGlyphs
{
    public const string WiFiFull = "\uE701";
    public const string Ethernet = "\uE839";
    public const string Offline = "\uF384";
    public const string Notifications = "\uEA8F";

    private const string Mute = "\uE74F";
    private const int Battery0 = 0xEBA0;          // EBA0..EBAA: %0..%100
    private const int BatteryCharging0 = 0xEBAB;  // EBAB..EBB5: şarjda %0..%100

    /// <summary>Ağ simgesi. Kablosuzda tam simge soluk çizilir, üstüne <see cref="WiFiLevel"/> dolu çubukları koyar.</summary>
    /// <remarks>Bağlı ama internet yoksa Windows gibi "bağlantı yok" küresi gösterilir.</remarks>
    public static string Network(NetworkStatus status) => status switch
    {
        { HasInternet: false } => Offline,
        { Kind: NetworkKind.WiFi or NetworkKind.Cellular } => WiFiFull,
        { Kind: NetworkKind.Ethernet } => Ethernet,
        _ => Offline
    };

    /// <summary>Kablosuz sinyalin dolu kısmı; kablosuz değilse ya da sinyal tamsa boş (tam simge yeterli).</summary>
    public static string WiFiLevel(NetworkStatus status)
    {
        if (!status.HasInternet || status.Kind is not (NetworkKind.WiFi or NetworkKind.Cellular))
            return string.Empty;

        // Windows 0-5 çubuk verir; simgede 3 kademe + tam var.
        return status.SignalBars switch
        {
            <= 1 => "\uE872",
            2 => "\uE873",
            3 or 4 => "\uE874",
            _ => string.Empty
        };
    }

    public static string Volume(float volume, bool muted)
    {
        if (muted || volume <= 0f)
            return Mute;

        return volume switch
        {
            < 0.34f => "\uE993",
            < 0.67f => "\uE994",
            _ => "\uE995"
        };
    }

    public static string Battery(int percent, bool charging)
    {
        var level = Math.Clamp((int)Math.Round(percent / 10.0), 0, 10);
        return char.ConvertFromUtf32((charging ? BatteryCharging0 : Battery0) + level);
    }
}

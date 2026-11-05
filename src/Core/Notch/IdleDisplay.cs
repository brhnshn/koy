namespace Koy.Core.Notch;

/// <summary>
/// Çentik boştayken (kapalı, müzik çalmıyor) ne gösterilir. Ayarda sayı olarak saklanır:
/// 0 = saat ve pil, 1 = yalnız saat, 2 = hiçbiri. Bilinmeyen değer varsayılana (saat ve pil) düşer.
/// </summary>
public static class IdleDisplay
{
    public const int ClockAndBattery = 0;
    public const int ClockOnly = 1;
    public const int Nothing = 2;

    public static bool ShowClock(int mode) => mode is not Nothing;

    public static bool ShowBattery(int mode) => mode is not (ClockOnly or Nothing);

    /// <summary>
    /// Wi-Fi / Bluetooth durum simgeleri pil gibi "ek bilgi"dir: yalnız "saat, pil ve bağlantılar" modunda görünür.
    /// "Yalnız saat" ve "Hiçbiri" seçimi kullanıcının çentikte başka bir şey istemediği anlamına gelir.
    /// </summary>
    public static bool ShowRadio(int mode) => mode is not (ClockOnly or Nothing);

    /// <summary>
    /// Üzerine gelince görünen tarih ve ağ simgesi de "ek bilgi"dir: yalnız "saat, pil ve bağlantılar" modunda çıkar.
    /// </summary>
    public static bool ShowPeekDetails(int mode) => mode is not (ClockOnly or Nothing);
}

namespace Koy.Core.Notch;

/// <summary>
/// Kapalı ve ipucu (Peek) durumlarında hangi yerleşimin çizileceği. Peek yalnız şarkı bilgisi için tasarlandı;
/// parça yokken fareyle üstüne gelmek boş bir ipucu çizmek yerine boşta yerleşimini (saat, pil, Wi-Fi / Bluetooth)
/// açık tutar. Böylece simgelere tıklanabilir ve ipucu metinleri görünür (simgeler fare çentiğe girer girmez
/// kaybolmaz).
/// </summary>
public static class NotchLayout
{
    /// <summary>Sabit pencere tuvalinin yüksekliği (kural #13); hiçbir panel bundan yüksek olamaz.</summary>
    public const double CanvasHeight = 260;

    /// <summary>Açık çentiğin genişliği (500 DIP'lik tuvalin içinde).</summary>
    public const double ExpandedWidth = 420;

    /// <summary>
    /// Açık çentiğin yüksekliği: üstteki 30 DIP'lik sekme satırı dahil. Takvim gün/hafta listesi ve araçlar için
    /// daha yüksektir. Hepsi tuvalin içinde kalır.
    /// </summary>
    public static double ExpandedHeight(NotchPanel panel) => panel switch
    {
        NotchPanel.Music => 142,
        NotchPanel.Calendar => 214,
        _ => 170
    };

    /// <summary>Boşta yerleşimi (saat, pil, durum simgeleri ya da müzik çalıyorsa kapak) çizilsin mi.</summary>
    public static bool ShowCollapsedLayout(NotchState state, bool hasTrack) =>
        state == NotchState.Collapsed || (state == NotchState.Peek && !hasTrack);

    /// <summary>
    /// Üzerine gelince (Peek) boşta yerleşimine tarih ve bağlantı simgeleri (ağ, Bluetooth) eklensin mi.
    /// Kapalı çentik kalabalıklaşmasın diye bu ayrıntılar yalnız Peek'te görünür; kapalıyken yalnız saat ve pil kalır.
    /// </summary>
    public static bool ShowPeekDetails(NotchState state, bool hasTrack) =>
        state == NotchState.Peek && !hasTrack;

    /// <summary>Şarkı adı ve sanatçıyı gösteren ipucu yerleşimi çizilsin mi.</summary>
    public static bool ShowPeekLayout(NotchState state, bool hasTrack) =>
        state == NotchState.Peek && hasTrack;
}

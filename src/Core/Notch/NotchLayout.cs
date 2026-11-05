namespace Koy.Core.Notch;

/// <summary>
/// Kapalı ve ipucu (Peek) durumlarında hangi yerleşimin çizileceği. Peek yalnız şarkı bilgisi için tasarlandı;
/// parça yokken fareyle üstüne gelmek boş bir ipucu çizmek yerine boşta yerleşimini (saat, pil, Wi-Fi / Bluetooth)
/// açık tutar. Böylece simgelere tıklanabilir ve ipucu metinleri görünür (simgeler fare çentiğe girer girmez
/// kaybolmaz).
/// </summary>
public static class NotchLayout
{
    /// <summary>Boşta yerleşimi (saat, pil, durum simgeleri ya da müzik çalıyorsa kapak) çizilsin mi.</summary>
    public static bool ShowCollapsedLayout(NotchState state, bool hasTrack) =>
        state == NotchState.Collapsed || (state == NotchState.Peek && !hasTrack);

    /// <summary>Şarkı adı ve sanatçıyı gösteren ipucu yerleşimi çizilsin mi.</summary>
    public static bool ShowPeekLayout(NotchState state, bool hasTrack) =>
        state == NotchState.Peek && hasTrack;
}

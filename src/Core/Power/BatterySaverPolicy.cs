namespace Koy.Core.Power;

/// <summary>Pil tasarrufu kararları (saf mantık, test edilebilir).</summary>
public static class BatterySaverPolicy
{
    /// <summary>
    /// Animasyonlar yalnız şu üç koşul birlikteyse kapatılır: kullanıcı "pil tasarrufunda animasyonları azalt"ı açmış,
    /// cihazda pil var ve şu an şarj olmadan pille çalışıyor. Prizde (şarj olan ya da dolu) tam animasyon sürer.
    /// </summary>
    public static bool ShouldReduceMotion(bool settingEnabled, bool hasBattery, BatteryStatus status) =>
        settingEnabled && hasBattery && status == BatteryStatus.Discharging;
}

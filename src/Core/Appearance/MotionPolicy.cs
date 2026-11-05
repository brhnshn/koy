namespace Koy.Core.Appearance;

/// <summary>
/// Animasyon hızı ve azaltılmış hareket kararları. Tek bir çarpan üretir: süreler bununla çarpılır,
/// 0 ise geçişler anında yapılır (Corvus'taki "motion scale" mantığı).
/// </summary>
public static class MotionPolicy
{
    public const string Calm = "Sakin";
    public const string Normal = "Normal";
    public const string Fast = "Hızlı";
    public const string Off = "Kapalı";

    /// <summary>Ayar metnini (Türkçe ya da İngilizce) tek biçime indirir; bilinmeyen değer Normal olur.</summary>
    public static string NormalizeSpeed(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v switch
        {
            "sakin" or "calm" or "slow" => Calm,
            "hızlı" or "hizli" or "fast" => Fast,
            "kapalı" or "kapali" or "off" or "none" => Off,
            _ => Normal
        };
    }

    /// <summary>Hız ayarının kendi çarpanı (azaltılmış hareket hesaba katılmadan).</summary>
    public static double SpeedFactor(string? value) =>
        NormalizeSpeed(value) switch
        {
            Calm => 1.5,
            Fast => 0.65,
            Off => 0.0,
            _ => 1.0
        };

    /// <summary>Son çarpan: 0 ise animasyon yok.</summary>
    /// <param name="speed">Animasyon ayarı (Sakin / Normal / Hızlı / Kapalı).</param>
    /// <param name="respectReduceMotion">"Windows'un animasyonları azalt ayarına uy" açık mı.</param>
    /// <param name="systemAnimationsEnabled">Windows istemci alanı animasyonları açık mı (SystemParameters.ClientAreaAnimation).</param>
    /// <param name="batterySaverReduceMotion">Pil tasarrufu politikası şu an hareketi azaltmayı istiyor mu (BatterySaverPolicy).</param>
    public static double Scale(string? speed, bool respectReduceMotion, bool systemAnimationsEnabled, bool batterySaverReduceMotion)
    {
        if (batterySaverReduceMotion)
            return 0.0;
        if (respectReduceMotion && !systemAnimationsEnabled)
            return 0.0;
        return SpeedFactor(speed);
    }

    /// <summary>Taban süreyi (ms) çarpanla ölçekler; 0 çarpan 0 ms verir.</summary>
    public static int ScaledMilliseconds(double baseMilliseconds, double scale) =>
        scale <= 0 ? 0 : (int)Math.Round(baseMilliseconds * scale);
}

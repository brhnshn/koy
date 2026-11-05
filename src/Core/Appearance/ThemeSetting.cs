namespace Koy.Core.Appearance;

/// <summary>Çözümlenmiş tema: ayar "Sistem" olsa bile ekrana bunlardan biri uygulanır.</summary>
public enum ResolvedTheme
{
    Dark,
    Light
}

/// <summary>
/// Theme ayarının (Sistem / Koyu / Açık) çözümlenmesi. Eski sürümler "Dark", "Koyu" gibi karışık
/// değerler yazmış olabilir; hepsi tek biçime indirilir ve bilinmeyen değer koyuya düşer.
/// </summary>
public static class ThemeSetting
{
    public const string System = "System";
    public const string Dark = "Dark";
    public const string Light = "Light";

    /// <summary>Ayar metnini "System", "Dark" ya da "Light" yapar (büyük/küçük harf ve Türkçe karşılıklar kabul edilir).</summary>
    public static string Normalize(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v switch
        {
            "system" or "sistem" => System,
            "light" or "açık" or "acik" => Light,
            _ => Dark
        };
    }

    /// <param name="value">Ayardaki tema metni.</param>
    /// <param name="systemUsesLightTheme">Windows uygulama teması açık mı (yalnız "Sistem" seçiliyken kullanılır).</param>
    public static ResolvedTheme Resolve(string? value, bool systemUsesLightTheme) =>
        Normalize(value) switch
        {
            System => systemUsesLightTheme ? ResolvedTheme.Light : ResolvedTheme.Dark,
            Light => ResolvedTheme.Light,
            _ => ResolvedTheme.Dark
        };
}

namespace Koy.Core.Appearance;

/// <summary>Bir vurgu renginden türetilen, arayüzde kullanılan tonlar (hepsi "#RRGGBB").</summary>
/// <param name="Base">Seçilen vurgu: anahtar izi, seçili gösterge, birincil düğme zemini.</param>
/// <param name="Hover">Üzerine gelince.</param>
/// <param name="Pressed">Basılıyken.</param>
/// <param name="OnAccent">Vurgu zemininin üstündeki yazı ve simge rengi (siyaha yakın ya da beyaz).</param>
/// <param name="Text">Vurgu renginde YAZI gerektiğinde kullanılan ton: yüzey üstünde en az 4,5:1 kontrast.</param>
/// <param name="Focus">Klavye odak halkası: yüzey üstünde en az 3:1 kontrast.</param>
public sealed record AccentPalette(string Base, string Hover, string Pressed, string OnAccent, string Text, string Focus)
{
    public const string DefaultHex = "#3B82F6";

    private static readonly Rgb Ink = new(0x0A, 0x0A, 0x0B);
    private static readonly Rgb White = new(0xFF, 0xFF, 0xFF);

    /// <param name="accentHex">Seçilen vurgu; geçersizse Koy mavisi kullanılır.</param>
    /// <param name="theme">Koyu temada tonlar açılır, açık temada koyulaşır.</param>
    /// <param name="surfaceHex">Yazının üstünde duracağı yüzey (Text ve Focus buna göre ayarlanır).</param>
    public static AccentPalette Derive(string? accentHex, ResolvedTheme theme, string surfaceHex)
    {
        var accent = Rgb.TryParse(accentHex, out var parsed) ? parsed : Rgb.Parse(DefaultHex);
        var surface = Rgb.Parse(surfaceHex);
        var dark = theme == ResolvedTheme.Dark;

        // Hover bir ton açılır (koyuda) ya da koyulaşır (açıkta); basılıyken ters yönde bir ton daha.
        var hover = ColorMath.Mix(accent, dark ? White : Ink, 0.18);
        var pressed = ColorMath.Mix(accent, dark ? Ink : Ink, dark ? 0.22 : 0.30);

        // Yazı ve simge için iki aday: siyaha yakın ya da beyaz; kontrastı yüksek olan seçilir.
        var onAccent = ColorMath.Contrast(accent, Ink) >= ColorMath.Contrast(accent, White) ? Ink : White;

        return new AccentPalette(
            accent.ToHex(),
            hover.ToHex(),
            pressed.ToHex(),
            onAccent.ToHex(),
            Readable(accent, surface, dark, 4.5).ToHex(),
            Readable(accent, surface, dark, 3.0).ToHex());
    }

    /// <summary>Vurgu rengini, yüzey üstünde istenen kontrasta ulaşana dek koyuda beyaza, açıkta siyaha doğru çeker.</summary>
    private static Rgb Readable(Rgb accent, Rgb surface, bool dark, double minContrast)
    {
        var target = dark ? White : Ink;
        for (var i = 0; i <= 20; i++)
        {
            var candidate = ColorMath.Mix(accent, target, i / 20.0);
            if (ColorMath.Contrast(candidate, surface) >= minContrast)
                return candidate;
        }

        return target;
    }
}

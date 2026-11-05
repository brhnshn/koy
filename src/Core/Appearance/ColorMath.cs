using System.Globalization;

namespace Koy.Core.Appearance;

/// <summary>sRGB renk (alfa yok; saydamlık ayrı tutulur). WCAG 2.x kontrast hesabı için.</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>"#RGB", "#RRGGBB" ya da "#AARRGGBB" metnini çözer; alfa yok sayılır.</summary>
    public static bool TryParse(string? hex, out Rgb color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        var s = hex.Trim().TrimStart('#');
        if (s.Length == 3)
            s = string.Concat(s.Select(c => new string(c, 2)));
        if (s.Length == 8)
            s = s[2..];
        if (s.Length != 6 || !int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            return false;

        color = new Rgb((byte)(v >> 16), (byte)(v >> 8), (byte)v);
        return true;
    }

    public static Rgb Parse(string hex) =>
        TryParse(hex, out var c) ? c : throw new FormatException($"Geçersiz renk: {hex}");
}

public static class ColorMath
{
    /// <summary>WCAG göreli parlaklık (0 siyah, 1 beyaz).</summary>
    public static double Luminance(Rgb c)
    {
        static double Channel(byte v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    /// <summary>WCAG kontrast oranı (1 ile 21 arası).</summary>
    public static double Contrast(Rgb a, Rgb b)
    {
        var la = Luminance(a);
        var lb = Luminance(b);
        var (hi, lo) = la >= lb ? (la, lb) : (lb, la);
        return (hi + 0.05) / (lo + 0.05);
    }

    public static double Contrast(string hexA, string hexB) => Contrast(Rgb.Parse(hexA), Rgb.Parse(hexB));

    /// <summary>a'dan b'ye t oranında karışım (0 = a, 1 = b).</summary>
    public static Rgb Mix(Rgb a, Rgb b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        byte M(byte x, byte y) => (byte)Math.Round(x + (y - x) * t);
        return new Rgb(M(a.R, b.R), M(a.G, b.G), M(a.B, b.B));
    }
}

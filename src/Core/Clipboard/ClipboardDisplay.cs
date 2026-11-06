using System.Globalization;
using System.Text.RegularExpressions;

namespace Koy.Core.Clipboard;

/// <summary>Öğenin ne kadar önce kopyalandığı (arayüz birimi Strings.resx'ten biçimlendirir).</summary>
public enum AgeUnit
{
    Now,
    Minutes,
    Hours,
    Days
}

/// <summary>Pano listesinin saf gösterim yardımcıları: önizleme, yaş, renk çözümleme. WPF yok.</summary>
public static partial class ClipboardDisplay
{
    /// <summary>1 dakikadan azı "şimdi", 1 saatten azı dakika, 1 günden azı saat, gerisi gün.</summary>
    public static (AgeUnit Unit, int Value) Age(DateTimeOffset copiedAt, DateTimeOffset now)
    {
        var span = now - copiedAt;
        if (span < TimeSpan.FromMinutes(1))
            return (AgeUnit.Now, 0);
        if (span < TimeSpan.FromHours(1))
            return (AgeUnit.Minutes, (int)span.TotalMinutes);
        if (span < TimeSpan.FromDays(1))
            return (AgeUnit.Hours, (int)span.TotalHours);
        return (AgeUnit.Days, (int)span.TotalDays);
    }

    /// <summary>
    /// Kartta gösterilecek ilk satırlar: boş satırlar atılır, her satırın başındaki girinti ve sondaki boşluk kırpılır,
    /// uzun satır <paramref name="maxLineChars"/> karakterde "…" ile kesilir; en çok <paramref name="maxLines"/> satır.
    /// </summary>
    public static string Preview(string? text, int maxLines = 3, int maxLineChars = 90)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(maxLines)
            .Select(l => l.Length > maxLineChars ? l[..maxLineChars].TrimEnd() + "…" : l);
        return string.Join("\n", lines);
    }

    /// <summary>"#RGB", "#RRGGBB", "#RRGGBBAA" (web sırası) ve "rgb()/rgba()" metnini bileşenlere çevirir.</summary>
    public static bool TryParseColor(string? text, out byte r, out byte g, out byte b, out byte a)
    {
        r = g = b = 0;
        a = 255;
        var t = text?.Trim();
        if (string.IsNullOrEmpty(t))
            return false;

        if (t[0] == '#')
        {
            var hex = t[1..];
            if (!hex.All(char.IsAsciiHexDigit))
                return false;
            switch (hex.Length)
            {
                case 3:
                    r = Nibble(hex[0]);
                    g = Nibble(hex[1]);
                    b = Nibble(hex[2]);
                    return true;
                case 6:
                case 8:
                    r = Byte(hex, 0);
                    g = Byte(hex, 2);
                    b = Byte(hex, 4);
                    if (hex.Length == 8)
                        a = Byte(hex, 6);
                    return true;
                default:
                    return false;
            }
        }

        var m = RgbParts().Match(t);
        if (!m.Success)
            return false;

        var rv = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var gv = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        var bv = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        if (rv > 255 || gv > 255 || bv > 255)
            return false;
        r = (byte)rv;
        g = (byte)gv;
        b = (byte)bv;
        if (m.Groups[4].Success && TryAlpha(m.Groups[4].Value, out var alpha))
            a = alpha;
        return true;
    }

    private static bool TryAlpha(string text, out byte alpha)
    {
        alpha = 255;
        var percent = text.EndsWith('%');
        var raw = percent ? text[..^1] : text;
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            return false;
        // CSS: 0-1 arası kesir ya da yüzde.
        var fraction = percent ? v / 100 : (v <= 1 ? v : v / 255);
        alpha = (byte)Math.Round(Math.Clamp(fraction, 0, 1) * 255);
        return true;
    }

    private static byte Nibble(char c) => (byte)(Convert.ToInt32(c.ToString(), 16) * 17);

    private static byte Byte(string hex, int index) => Convert.ToByte(hex.Substring(index, 2), 16);

    [GeneratedRegex(@"^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*(?:,\s*(\d{1,3}%?|\d*\.\d+)\s*)?\)$", RegexOptions.IgnoreCase)]
    private static partial Regex RgbParts();
}

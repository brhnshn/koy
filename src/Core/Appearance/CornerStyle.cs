namespace Koy.Core.Appearance;

public enum CornerStyle
{
    Sharp,
    Standard,
    Round
}

/// <summary>Koy penceresinin köşe yarıçapları (DIP). Pencere dışındaki yüzeyler (çentik, dock) bundan etkilenmez.</summary>
public readonly record struct CornerRadii(double Window, double Row, double Control, double Small)
{
    public static CornerRadii For(CornerStyle style) => style switch
    {
        CornerStyle.Sharp => new CornerRadii(4, 4, 3, 2),
        CornerStyle.Round => new CornerRadii(18, 12, 10, 6),
        _ => new CornerRadii(12, 8, 6, 4)
    };
}

public static class CornerSetting
{
    public const double SharpValue = 6;
    public const double StandardValue = 12;
    public const double RoundValue = 18;

    /// <summary>
    /// Ayardaki sayıyı seçeneğe çevirir. Eski sürümlerin varsayılanı 18 idi ve hiçbir yerde kullanılmıyordu;
    /// bu yüzden 18 "Yuvarlak" seçimi sayılmaz, Standart olarak okunur. Yuvarlak 17 olarak saklanır.
    /// </summary>
    public static CornerStyle Resolve(double stored)
    {
        if (stored <= 0)
            return CornerStyle.Standard;
        if (stored <= 8)
            return CornerStyle.Sharp;
        if (stored is > 14 and < 18 || stored > 18)
            return CornerStyle.Round;
        return CornerStyle.Standard;
    }

    /// <summary>Seçeneğin ayar dosyasına yazılan değeri.</summary>
    public static double ToStored(CornerStyle style) => style switch
    {
        CornerStyle.Sharp => SharpValue,
        CornerStyle.Round => 17,
        _ => StandardValue
    };
}

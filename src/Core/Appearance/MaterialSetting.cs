namespace Koy.Core.Appearance;

/// <summary>Malzeme ayarı: Katı (opak yüzeyler) ya da Yarı saydam.</summary>
public static class MaterialSetting
{
    public const string Solid = "Katı";
    public const string Translucent = "Yarı saydam";

    /// <summary>Yarı saydamda pencere yüzeylerinin opaklığı: DESIGN'daki dock aralığının (%88–96) alt ucu.</summary>
    public const double TranslucentSurfaceOpacity = 0.88;

    public static bool IsTranslucent(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v is "yarı saydam" or "yari saydam" or "translucent" or "acrylic" or "mica";
    }

    public static string Normalize(string? value) => IsTranslucent(value) ? Translucent : Solid;
}

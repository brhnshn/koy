namespace Koy.Core.Media;

/// <summary>
/// Medya oturumunun kaynak kimliğini (AUMID ya da exe yolu) kullanıcıya okunur bir uygulama adına çevirir.
/// </summary>
public static class MediaAppName
{
    // Paket ya da exe adı → ürün adı. Listede olmayan kimlikler için ad kimlikten türetilir.
    private static readonly Dictionary<string, string> KnownApps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Microsoft.ZuneMusic"] = "Media Player",
        ["Microsoft.ZuneVideo"] = "Movies & TV",
        ["SpotifyAB.SpotifyMusic"] = "Spotify",
        ["Spotify"] = "Spotify",
        ["chrome"] = "Chrome",
        ["msedge"] = "Edge",
        ["firefox"] = "Firefox",
        ["opera"] = "Opera",
        ["brave"] = "Brave",
        ["vlc"] = "VLC",
        ["AppleInc.AppleMusicWin"] = "Apple Music",
    };

    public static string FromSourceAppId(string? sourceAppId)
    {
        if (string.IsNullOrWhiteSpace(sourceAppId))
            return string.Empty;

        var id = sourceAppId.Trim();

        // Store uygulaması: "Paket.Adı_yayıncıKarması!UygulamaKimliği"
        var bang = id.IndexOf('!');
        if (bang >= 0)
        {
            var package = id[..bang];
            var underscore = package.IndexOf('_');
            if (underscore > 0)
                package = package[..underscore];

            if (KnownApps.TryGetValue(package, out var known))
                return known;

            // "Yayıncı.Uygulama" → "Uygulama"
            var dot = package.LastIndexOf('.');
            return dot >= 0 && dot < package.Length - 1 ? package[(dot + 1)..] : package;
        }

        // Masaüstü uygulaması: tam yol ya da "chrome.exe"
        var slash = id.LastIndexOfAny(['\\', '/']);
        var name = slash >= 0 ? id[(slash + 1)..] : id;
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];

        if (KnownApps.TryGetValue(name, out var knownExe))
            return knownExe;

        return name.Length > 0 ? char.ToUpperInvariant(name[0]) + name[1..] : name;
    }
}

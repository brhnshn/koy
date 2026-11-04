using Koy.Core.Media;

namespace Koy.Core.Tests;

public class MediaAppNameTests
{
    [Theory]
    [InlineData("Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic", "Media Player")]
    [InlineData("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", "Spotify")]
    [InlineData("Contoso.PodcastPlayer_abc123!App", "PodcastPlayer")]
    [InlineData("chrome.exe", "Chrome")]
    [InlineData(@"C:\Program Files\VideoLAN\VLC\vlc.exe", "VLC")]
    [InlineData("Spotify.exe", "Spotify")]
    [InlineData("foobar2000.exe", "Foobar2000")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void FromSourceAppId_ReturnsReadableName(string? id, string expected)
    {
        Assert.Equal(expected, MediaAppName.FromSourceAppId(id));
    }
}

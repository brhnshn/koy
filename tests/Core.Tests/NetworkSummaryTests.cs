using Koy.Core.Network;

namespace Koy.Core.Tests;

public class NetworkSummaryTests
{
    [Fact]
    public void Disconnected_status_is_described_as_disconnected()
    {
        Assert.Equal(NetworkDisplay.Disconnected, NetworkSummary.Describe(NetworkStatus.Disconnected));
        Assert.False(NetworkSummary.IsConnected(NetworkStatus.Disconnected));
    }

    [Fact]
    public void Connection_without_internet_is_flagged_even_for_wifi()
    {
        var status = new NetworkStatus(NetworkKind.WiFi, false, 3, "Ev");

        Assert.Equal(NetworkDisplay.NoInternet, NetworkSummary.Describe(status));
        Assert.False(NetworkSummary.IsConnected(status));
    }

    [Theory]
    [InlineData(NetworkKind.WiFi, NetworkDisplay.WiFi)]
    [InlineData(NetworkKind.Ethernet, NetworkDisplay.Ethernet)]
    [InlineData(NetworkKind.Cellular, NetworkDisplay.Cellular)]
    public void Connected_kinds_map_to_their_display(NetworkKind kind, NetworkDisplay expected)
    {
        var status = new NetworkStatus(kind, true, 4);

        Assert.Equal(expected, NetworkSummary.Describe(status));
        Assert.True(NetworkSummary.IsConnected(status));
    }

    [Fact]
    public void WiFi_name_is_trimmed_and_only_given_for_wifi()
    {
        Assert.Equal("Ev Ağı", NetworkSummary.WiFiName(new NetworkStatus(NetworkKind.WiFi, true, 4, "  Ev Ağı ")));
        Assert.Null(NetworkSummary.WiFiName(new NetworkStatus(NetworkKind.WiFi, true, 4, "   ")));
        Assert.Null(NetworkSummary.WiFiName(new NetworkStatus(NetworkKind.WiFi, true, 4)));
        Assert.Null(NetworkSummary.WiFiName(new NetworkStatus(NetworkKind.Ethernet, true, 0, "Ethernet 2")));
    }

    [Fact]
    public void Status_with_a_different_name_is_a_different_status()
    {
        // Servis yalnız durum değişince olay verir; ağ adı değişince de verilmeli (ev -> ofis aynı tür, aynı sinyal).
        Assert.NotEqual(new NetworkStatus(NetworkKind.WiFi, true, 4, "Ev"), new NetworkStatus(NetworkKind.WiFi, true, 4, "Ofis"));
    }
}

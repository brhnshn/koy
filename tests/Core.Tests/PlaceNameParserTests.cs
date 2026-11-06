using Koy.Core.Weather;
using Xunit;

namespace Koy.Core.Tests;

public class PlaceNameParserTests
{
    [Fact]
    public void Sehir_adi_once_gelir()
    {
        const string json = """{"address":{"suburb":"Kadıköy","city":"İstanbul","state":"İstanbul","country":"Türkiye"}}""";
        Assert.Equal("İstanbul", PlaceNameParser.Parse(json));
    }

    [Fact]
    public void Sehir_yoksa_kasaba_sonra_ilce_sonra_il()
    {
        Assert.Equal("Urla", PlaceNameParser.Parse("""{"address":{"town":"Urla","province":"İzmir"}}"""));
        Assert.Equal("Seferihisar", PlaceNameParser.Parse("""{"address":{"county":"Seferihisar","province":"İzmir"}}"""));
        Assert.Equal("İzmir", PlaceNameParser.Parse("""{"address":{"province":"İzmir","country":"Türkiye"}}"""));
    }

    [Fact]
    public void Sokak_ve_mahalle_kullanilmaz()
    {
        Assert.Null(PlaceNameParser.Parse("""{"address":{"road":"Bağdat Caddesi","suburb":"Kadıköy","country":"Türkiye"}}"""));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bozuk")]
    [InlineData("""{"error":"Unable to geocode"}""")]
    [InlineData("""{"address":{"city":"  "}}""")]
    [InlineData("""{"address":"metin"}""")]
    [InlineData("[]")]
    public void Gecersiz_yanit_null_doner(string? json) => Assert.Null(PlaceNameParser.Parse(json));

    [Fact]
    public void Koordinat_iki_ondaliga_yuvarlanir()
    {
        Assert.Equal((41.01, 28.98), PlaceNameParser.Round(41.00823, 28.97836));
        Assert.Equal((-33.87, 151.21), PlaceNameParser.Round(-33.8688, 151.2093));
    }

    [Fact]
    public void Adres_yuvarlanmis_koordinat_ve_dil_tasir()
    {
        var url = PlaceNameParser.BuildUrl(41.00823, 28.97836, "tr");
        Assert.StartsWith("https://nominatim.openstreetmap.org/reverse?", url);
        Assert.Contains("lat=41.01", url);
        Assert.Contains("lon=28.98", url);
        Assert.Contains("zoom=10", url);
        Assert.Contains("accept-language=tr", url);
        Assert.DoesNotContain("41.008", url);

        Assert.Contains("accept-language=en", PlaceNameParser.BuildUrl(1, 2, "EN"));
        Assert.Contains("accept-language=tr", PlaceNameParser.BuildUrl(1, 2, "de"));
    }
}

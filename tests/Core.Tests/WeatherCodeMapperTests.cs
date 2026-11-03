using Koy.Core.Weather;

namespace Koy.Core.Tests;

public sealed class WeatherCodeMapperTests
{
    [Theory]
    [InlineData(0, "Açık", "Sun")]
    [InlineData(1, "Çoğunlukla Açık", "PartlyCloudy")]
    [InlineData(2, "Parçalı Bulutlu", "PartlyCloudy")]
    [InlineData(3, "Kapalı", "Cloud")]
    [InlineData(45, "Sisli", "Fog")]
    [InlineData(61, "Hafif Yağmurlu", "Rain")]
    [InlineData(71, "Hafif Kar Yağışlı", "Snow")]
    [InlineData(95, "Gök Gürültülü Fırtına", "Thunderstorm")]
    [InlineData(999, "Bilinmiyor", "Cloud")]
    public void Maps_Wmo_Codes_Correctly(int code, string expectedDescription, string expectedIcon)
    {
        Assert.Equal(expectedDescription, WeatherCodeMapper.ToDescription(code));
        Assert.Equal(expectedIcon, WeatherCodeMapper.ToIconKind(code));
    }
}

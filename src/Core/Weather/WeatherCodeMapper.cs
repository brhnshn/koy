namespace Koy.Core.Weather;

public static class WeatherCodeMapper
{
    public static string ToDescription(int code) => code switch
    {
        0 => "Açık",
        1 => "Çoğunlukla Açık",
        2 => "Parçalı Bulutlu",
        3 => "Kapalı",
        45 => "Sisli",
        48 => "Kırağılı Sis",
        51 => "Hafif Çisenti",
        53 => "Orta Çisenti",
        55 => "Yoğun Çisenti",
        56 or 57 => "Dondurucu Çisenti",
        61 => "Hafif Yağmurlu",
        63 => "Yağmurlu",
        65 => "Kuvvetli Yağmurlu",
        66 or 67 => "Dondurucu Yağmur",
        71 => "Hafif Kar Yağışlı",
        73 => "Kar Yağışlı",
        75 => "Yoğun Kar Yağışlı",
        77 => "Kar Taneli",
        80 => "Hafif Sağanak",
        81 => "Sağanak Yağışlı",
        82 => "Kuvvetli Sağanak",
        85 => "Hafif Kar Sağanağı",
        86 => "Kuvvetli Kar Sağanağı",
        95 => "Gök Gürültülü Fırtına",
        96 or 99 => "Dolulu Fırtına",
        _ => "Bilinmiyor"
    };

    public static string ToIconKind(int code) => code switch
    {
        0 => "Sun",
        1 or 2 => "PartlyCloudy",
        3 => "Cloud",
        45 or 48 => "Fog",
        51 or 53 or 55 or 56 or 57 => "Drizzle",
        61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => "Rain",
        71 or 73 or 75 or 77 or 85 or 86 => "Snow",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Cloud"
    };
}

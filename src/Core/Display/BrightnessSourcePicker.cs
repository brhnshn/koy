namespace Koy.Core.Display;

/// <summary>Parlaklığı hangi yolla ayarlayacağımız.</summary>
public enum BrightnessSourceKind
{
    /// <summary>Hiçbir yol yok: kaydırıcı devre dışı kalır.</summary>
    None,

    /// <summary>Dahili ekran: WMI (WmiMonitorBrightness / WmiSetBrightness).</summary>
    Wmi,

    /// <summary>Harici monitör: DDC/CI (Get/SetMonitorBrightness).</summary>
    Ddc
}

/// <summary>Çentiğin bulunduğu ekranın türü.</summary>
public enum BrightnessTarget
{
    /// <summary>Ekran bilinmiyor (sağlayıcı yok ya da eşleşmedi).</summary>
    Unknown,

    /// <summary>Dizüstü paneli (DisplayConfig çıkış teknolojisi dahili / gömülü).</summary>
    Internal,

    /// <summary>Harici monitör.</summary>
    External
}

/// <summary>
/// Kaynak seçimi: karar ekran düzeni her değiştiğinde yeniden verilir (servis bir kez kurulup ekran
/// kapanınca ölü monitör tanıtıcısında takılmasın). WMI yalnız dahili paneli ayarlar; harici ekranda
/// kullanılırsa başka ekranın parlaklığını değiştirirdi, bu yüzden harici hedefte yalnız DDC denenir.
/// </summary>
public static class BrightnessSourcePicker
{
    public static BrightnessSourceKind Pick(BrightnessTarget target, bool wmiAvailable, bool ddcAvailable) => target switch
    {
        BrightnessTarget.Internal => wmiAvailable ? BrightnessSourceKind.Wmi
            : ddcAvailable ? BrightnessSourceKind.Ddc : BrightnessSourceKind.None,
        BrightnessTarget.External => ddcAvailable ? BrightnessSourceKind.Ddc : BrightnessSourceKind.None,
        _ => wmiAvailable ? BrightnessSourceKind.Wmi
            : ddcAvailable ? BrightnessSourceKind.Ddc : BrightnessSourceKind.None
    };

    /// <summary>DDC/CI değerini (min..max) yüzdeye çevirir. Aralık bozuksa değeri 0-100'e kıstırır.</summary>
    public static int ToPercent(int value, int min, int max) =>
        max > min ? (int)Math.Round((Math.Clamp(value, min, max) - min) * 100.0 / (max - min)) : Math.Clamp(value, 0, 100);

    /// <summary>Yüzdeyi DDC/CI aralığına (min..max) çevirir.</summary>
    public static int FromPercent(int percent, int min, int max) =>
        max > min ? min + (int)Math.Round(Math.Clamp(percent, 0, 100) * (max - min) / 100.0) : Math.Clamp(percent, 0, 100);
}

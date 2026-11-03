namespace Koy.Core.Display;

/// <summary>
/// Ekran parlaklığı kontrol sözleşmesi.
/// Dahili ekranlar (WMI) veya harici monitörler (DDC/CI) için.
/// Desteklenmiyorsa IsSupported false döner.
/// </summary>
public interface IBrightnessService : IDisposable
{
    bool IsSupported { get; }
    int Brightness { get; }

    event EventHandler<int>? BrightnessChanged;

    void SetBrightness(int brightness);
}

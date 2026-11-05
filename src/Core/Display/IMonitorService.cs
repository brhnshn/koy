using Koy.Core.Shell;

namespace Koy.Core.Display;

/// <summary>
/// Bir ekranın fiziksel ve DIP boyutlarını, DPI ölçeğini ve birincil durumunu temsil eden model.
/// <see cref="Id"/> kararlı kimliktir (DisplayConfig monitorDevicePath; çözünürlük ya da sıra değişince değişmez),
/// <see cref="Name"/> dostça addır, <see cref="DeviceName"/> Windows'un geçici aygıt adıdır (örn. DISPLAY2).
/// </summary>
public sealed record DisplayMonitor(
    string Id,
    string Name,
    WindowRect Bounds,
    WindowRect WorkArea,
    bool IsPrimary,
    double DpiScaleX = 1.0,
    double DpiScaleY = 1.0,
    nint Handle = 0,
    string DeviceName = "")
{
    /// <summary>Piksel cinsinden çözünürlük genişliği.</summary>
    public int PixelWidth => Bounds.Width;

    /// <summary>Piksel cinsinden çözünürlük yüksekliği.</summary>
    public int PixelHeight => Bounds.Height;

    /// <summary>
    /// Windows'un "Ekranı tanımla" ile gösterdiği numara (aygıt adının sonundaki sayı: DISPLAY2 → 2).
    /// Aygıt adı yoksa 0.
    /// </summary>
    public int Number
    {
        get
        {
            var end = DeviceName.Length;
            var start = end;
            while (start > 0 && char.IsAsciiDigit(DeviceName[start - 1]))
                start--;
            return start < end && int.TryParse(DeviceName.AsSpan(start, end - start), out var n) ? n : 0;
        }
    }

    /// <summary>DIP cinsinden genişlik.</summary>
    public double WidthDip => (Bounds.Right - Bounds.Left) / DpiScaleX;

    /// <summary>DIP cinsinden yükseklik.</summary>
    public double HeightDip => (Bounds.Bottom - Bounds.Top) / DpiScaleY;

    /// <summary>DIP cinsinden sol koordinat.</summary>
    public double LeftDip => Bounds.Left / DpiScaleX;

    /// <summary>DIP cinsinden üst koordinat.</summary>
    public double TopDip => Bounds.Top / DpiScaleY;
}

/// <summary>
/// Çentik veya Dock'un hedef monitör seçim modu.
/// </summary>
public enum MonitorTargetMode
{
    Primary,
    All,
    Specific
}

/// <summary>
/// Bağlı monitörleri sorgulama ve ekran değişikliklerini izleme sözleşmesi.
/// </summary>
public interface IMonitorService : IDisposable
{
    event EventHandler? MonitorsChanged;

    IReadOnlyList<DisplayMonitor> GetMonitors();

    DisplayMonitor? GetPrimaryMonitor();

    DisplayMonitor? GetMonitor(string idOrName);

    void Start();

    void Stop();

    void NotifyDisplayChanged() { }
}

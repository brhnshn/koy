namespace Koy.Core.Display;

/// <summary>Yeni bağlanan (daha önce hiç görülmemiş) ekranda çentik ve dock'un durumu.</summary>
public enum NewScreenMode
{
    /// <summary>Çentik ve dock açık.</summary>
    NotchAndDock,

    /// <summary>Yalnız çentik açık.</summary>
    NotchOnly,

    /// <summary>İkisi de kapalı.</summary>
    None
}

/// <summary>
/// Tek bir monitörün ayarı. Anahtar <see cref="DisplayMonitor.Id"/> (kararlı kimlik) olarak
/// <c>AppSettings.Monitors</c> sözlüğünde durur. Ana ekran bilgisi saklanmaz: salt bilgidir, her açılışta sorgulanır.
/// </summary>
public sealed record MonitorSettings
{
    public bool NotchEnabled { get; set; } = true;
    public bool DockEnabled { get; set; } = true;

    /// <summary>Son görülen dostça ad; ekran ayrıldığında da ayarın kime ait olduğu belli olsun diye.</summary>
    public string? Name { get; set; }
}

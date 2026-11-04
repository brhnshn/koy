namespace Koy.Core.Shell;

/// <summary>
/// Tam ekran uygulama (oyun, sunum, video) algılama sözleşmesi.
/// </summary>
public interface IFullscreenDetector
{
    /// <summary>
    /// Sistemde tam ekran bir uygulama olup olmadığını denetler.
    /// </summary>
    bool IsFullscreen();

    /// <summary>
    /// Belirtilen monitörde tam ekran bir uygulama olup olmadığını denetler.
    /// </summary>
    bool IsFullscreen(nint targetMonitorHandle) => IsFullscreen();
}

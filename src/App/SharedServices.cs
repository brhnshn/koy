using Koy.Platform.Media;
using Koy.Platform.Shell;

namespace Koy.App;

/// <summary>
/// Dock ve Notch'un ortak kullandığı platform servislerinin tek örneği (WinEvent hook'ları ve GSMTC oturumu ikişer kez kurulmasın).
/// Örnek ilk isteyen özellik için oluşur, son kullanan bıraktığında Dispose edilir; böylece özellikler kapalıyken
/// hiçbir şey belleğe yüklenmez (AGENTS.md #5, #12). Yalnız UI thread'inden kullanılır.
/// </summary>
public sealed class SharedServices : IDisposable
{
    private WindowTracker? _windowTracker;
    private int _windowTrackerUsers;
    private MediaService? _mediaService;
    private int _mediaServiceUsers;
    private bool _disposed;

    public WindowTracker AcquireWindowTracker()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _windowTrackerUsers++;
        return _windowTracker ??= new WindowTracker();
    }

    public void ReleaseWindowTracker()
    {
        if (_windowTrackerUsers == 0)
            return;

        if (--_windowTrackerUsers == 0)
        {
            _windowTracker?.Dispose();
            _windowTracker = null;
        }
    }

    /// <summary>Çalışan medya servisi (yoksa null); kısayol gibi tek seferlik işler yeni servis kurmasın diye.</summary>
    public MediaService? RunningMediaService => _mediaService;

    public MediaService AcquireMediaService()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _mediaServiceUsers++;
        if (_mediaService is null)
        {
            _mediaService = new MediaService();
            _ = _mediaService.StartAsync();
        }

        return _mediaService;
    }

    public void ReleaseMediaService()
    {
        if (_mediaServiceUsers == 0)
            return;

        if (--_mediaServiceUsers == 0)
        {
            _mediaService?.Dispose();
            _mediaService = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _windowTracker?.Dispose();
        _windowTracker = null;
        _mediaService?.Dispose();
        _mediaService = null;
        _windowTrackerUsers = 0;
        _mediaServiceUsers = 0;
    }
}

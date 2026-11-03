using Koy.Core.Dock;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Koy.Platform.Shell;

/// <summary>
/// Dock penceresini Windows AppBar olarak kaydeder, ekranın altında masaüstü çalışma alanı ayırır
/// ve kapanışta (ABM_REMOVE) eski çalışma alanını geri yükler.
/// </summary>
public sealed class AppBarService : IAppBarService
{
    private const uint ABM_NEW = 0x00000000;
    private const uint ABM_REMOVE = 0x00000001;
    private const uint ABM_QUERYPOS = 0x00000002;
    private const uint ABM_SETPOS = 0x00000003;
    private const uint ABE_BOTTOM = 3;

    private readonly HashSet<nint> _registeredHandles = [];
    private bool _disposed;

    public unsafe void Register(nint windowHandle, int heightDip, double dpiScale = 1.0)
    {
        if (windowHandle == 0 || _registeredHandles.Contains(windowHandle))
            return;

        var hwnd = new HWND(windowHandle);

        var data = new APPBARDATA
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = hwnd,
            uCallbackMessage = 0x0400 + 100 // WM_USER + 100
        };

        var registered = PInvoke.SHAppBarMessage(ABM_NEW, ref data);
        if (registered != 0)
        {
            _registeredHandles.Add(windowHandle);
            UpdatePositionInternal(hwnd, heightDip, dpiScale);
        }
    }

    public unsafe void UpdatePosition(nint windowHandle, int heightDip, double dpiScale = 1.0)
    {
        if (windowHandle == 0 || !_registeredHandles.Contains(windowHandle))
            return;

        UpdatePositionInternal(new HWND(windowHandle), heightDip, dpiScale);
    }

    public unsafe void Unregister(nint windowHandle)
    {
        if (windowHandle == 0 || !_registeredHandles.Remove(windowHandle))
            return;

        var hwnd = new HWND(windowHandle);
        var data = new APPBARDATA
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = hwnd
        };

        PInvoke.SHAppBarMessage(ABM_REMOVE, ref data);
    }

    private static unsafe void UpdatePositionInternal(HWND hwnd, int heightDip, double dpiScale)
    {
        var screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
        var screenHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
        var heightPx = (int)(heightDip * dpiScale);

        var data = new APPBARDATA
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = hwnd,
            uEdge = ABE_BOTTOM,
            rc = new RECT
            {
                left = 0,
                top = screenHeight - heightPx,
                right = screenWidth,
                bottom = screenHeight
            }
        };

        PInvoke.SHAppBarMessage(ABM_QUERYPOS, ref data);
        data.rc.top = data.rc.bottom - heightPx;
        PInvoke.SHAppBarMessage(ABM_SETPOS, ref data);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var handles = _registeredHandles.ToList();
        foreach (var handle in handles)
        {
            Unregister(handle);
        }
    }
}

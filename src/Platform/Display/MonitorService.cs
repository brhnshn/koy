using Koy.Core.Display;
using Koy.Core.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;

namespace Koy.Platform.Display;

/// <summary>
/// Bağlı monitörleri EnumDisplayMonitors ve GetDpiForMonitor ile sorgulayan,
/// çözünürlük veya monitör değişikliklerinde (WM_DISPLAYCHANGE) pencerelerin yeniden konumlandırılmasını sağlayan servis.
/// </summary>
public sealed class MonitorService : IMonitorService
{
    private const uint MONITORINFOF_PRIMARY = 1;
    private bool _started;
    private bool _disposed;

    public event EventHandler? MonitorsChanged;

    public void Start()
    {
        _started = true;
    }

    public void Stop()
    {
        _started = false;
    }

    public void NotifyDisplayChanged()
    {
        if (!_started || _disposed) return;
        MonitorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public unsafe IReadOnlyList<DisplayMonitor> GetMonitors()
    {
        var list = new List<DisplayMonitor>();

        PInvoke.EnumDisplayMonitors(
            default(HDC),
            (RECT*)null,
            (HMONITOR hMonitor, HDC _, RECT* _, LPARAM _) =>
            {
                var mi = new MONITORINFOEXW
                {
                    monitorInfo = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFOEXW) }
                };

                if (PInvoke.GetMonitorInfo(hMonitor, (MONITORINFO*)&mi))
                {
                    var isPrimary = (mi.monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0;
                    var deviceName = mi.szDevice.ToString();

                    // DPI ölçeği oku (varsayılan 96 DPI = 1.0)
                    double dpiScaleX = 1.0;
                    double dpiScaleY = 1.0;

                    try
                    {
                        uint dpiX = 96, dpiY = 96;
                        if (PInvoke.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, &dpiX, &dpiY) == 0)
                        {
                            dpiScaleX = dpiX / 96.0;
                            dpiScaleY = dpiY / 96.0;
                        }
                    }
                    catch
                    {
                        // DPI API hatası durumunda 1.0 kullanılır
                    }

                    var bounds = new WindowRect(
                        mi.monitorInfo.rcMonitor.left,
                        mi.monitorInfo.rcMonitor.top,
                        mi.monitorInfo.rcMonitor.right,
                        mi.monitorInfo.rcMonitor.bottom);

                    var workArea = new WindowRect(
                        mi.monitorInfo.rcWork.left,
                        mi.monitorInfo.rcWork.top,
                        mi.monitorInfo.rcWork.right,
                        mi.monitorInfo.rcWork.bottom);

                    list.Add(new DisplayMonitor(
                        Id: deviceName,
                        Name: deviceName,
                        Bounds: bounds,
                        WorkArea: workArea,
                        IsPrimary: isPrimary,
                        DpiScaleX: dpiScaleX,
                        DpiScaleY: dpiScaleY,
                        Handle: hMonitor.Value));
                }

                return true;
            }, 0);

        return list;
    }

    public DisplayMonitor? GetPrimaryMonitor()
    {
        var monitors = GetMonitors();
        return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
    }

    public DisplayMonitor? GetMonitor(string idOrName)
    {
        if (string.IsNullOrWhiteSpace(idOrName))
            return GetPrimaryMonitor();

        var monitors = GetMonitors();
        return monitors.FirstOrDefault(m =>
            string.Equals(m.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.Name, idOrName, StringComparison.OrdinalIgnoreCase)) ?? GetPrimaryMonitor();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Koy.Platform.Shell;

namespace Koy.App.Features.Dock.Preview;

/// <summary>
/// Canlı DWM pencere küçük resmini (DwmRegisterThumbnail) WPF içinde görüntüleyen HwndHost bileşeni.
/// Kapanışta ve fare ayrıldığında DwmUnregisterThumbnail çağırarak sıfır bellek sızıntısı sağlar.
/// </summary>
public sealed class WindowThumbnailHost : HwndHost
{
    private readonly nint _sourceHwnd;
    private nint _thumbHandle;
    private nint _childHwnd;

    public WindowThumbnailHost(nint sourceHwnd)
    {
        _sourceHwnd = sourceHwnd;
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        var width = (int)Math.Max(10, ActualWidth > 0 ? ActualWidth : 200);
        var height = (int)Math.Max(10, ActualHeight > 0 ? ActualHeight : 120);

        _childHwnd = DwmThumbnailHelper.CreateChildHostWindow(hwndParent.Handle, width, height);

        if (_childHwnd != 0 && _sourceHwnd != 0)
        {
            _thumbHandle = DwmThumbnailHelper.Register(_childHwnd, _sourceHwnd);
            UpdateThumbnail(width, height);
        }

        return new HandleRef(this, _childHwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        if (_thumbHandle != 0)
        {
            DwmThumbnailHelper.Unregister(_thumbHandle);
            _thumbHandle = 0;
        }

        if (_childHwnd != 0)
        {
            DwmThumbnailHelper.DestroyChildHostWindow(_childHwnd);
            _childHwnd = 0;
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (_childHwnd != 0 && sizeInfo.NewSize.Width > 0 && sizeInfo.NewSize.Height > 0)
        {
            var w = (int)sizeInfo.NewSize.Width;
            var h = (int)sizeInfo.NewSize.Height;
            DwmThumbnailHelper.ResizeChildHostWindow(_childHwnd, w, h);
            UpdateThumbnail(w, h);
        }
    }

    private void UpdateThumbnail(int width, int height)
    {
        if (_thumbHandle == 0 || width <= 0 || height <= 0)
            return;

        DwmThumbnailHelper.Update(_thumbHandle, width, height);
    }
}

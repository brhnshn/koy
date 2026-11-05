using System.Windows.Interop;
using System.Windows.Threading;
using Koy.App.Features.Settings;
using Koy.Core.Diagnostics;
using Koy.Platform.Shell;

namespace Koy.App.Features.Menu;

/// <summary>
/// Sistem tepsisi (system tray) simgesini ve gelen tıklama olaylarını yönetir.
/// Sol tek tık veya sağ tık: Sağ Alt Menüyü açar.
/// Çift tık: Doğrudan Koy ana penceresini (Ayarlar / Dashboard) açar.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private const uint WmTrayCallback = 0x8001;
    private const int WmLButtonUp = 0x0202;
    private const int WmLButtonDblClk = 0x0203;
    private const int WmRButtonUp = 0x0205;

    private HwndSource? _hwndSource;
    private TrayIconService? _trayService;
    private DispatcherTimer? _singleClickTimer;
    private bool _isRunning;
    private readonly uint _taskbarCreatedMessage = TrayIconService.TaskbarCreatedMessage;

    /// <summary>Explorer yeniden başladı; simge geri eklendi. Dock görev çubuğu gizlemesini yeniden uygulamalı.</summary>
    public event Action? ExplorerRestarted;

    public void Start()
    {
        if (_isRunning) return;

        var parameters = new HwndSourceParameters("KoyTrayListener")
        {
            WindowStyle = 0,
            Width = 0,
            Height = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        _singleClickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };
        _singleClickTimer.Tick += (_, _) =>
        {
            _singleClickTimer.Stop();
            CrashLog.Guard("Tepsi tek tık", SagAltMenuWindow.Toggle);
        };

        _trayService = new TrayIconService(_hwndSource.Handle, WmTrayCallback);
        _trayService.Add("Koy");
        _isRunning = true;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        // WndProc'tan sızan istisna süreci STATUS_FATAL_USER_CALLBACK_EXCEPTION ile düşürür; logla ve yut.
        try
        {
            return HandleMessage(msg, lParam, ref handled);
        }
        catch (Exception ex)
        {
            CrashLog.Write("TrayIconManager.WndProc", ex);
            return 0;
        }
    }

    private nint HandleMessage(int msg, nint lParam, ref bool handled)
    {
        if (_taskbarCreatedMessage != 0 && (uint)msg == _taskbarCreatedMessage)
        {
            _trayService?.Recreate();
            ExplorerRestarted?.Invoke();
            return 0;
        }

        if (msg == WmTrayCallback)
        {
            var mouseMsg = lParam.ToInt32();

            if (mouseMsg == WmLButtonDblClk)
            {
                // Çift tık: Tek tık zamanlayıcısını durdur, menüyü kapat ve Koy ana penceresini aç
                _singleClickTimer?.Stop();
                SagAltMenuWindow.Instance.Hide();
                KoyWindow.ShowInstance();
                handled = true;
            }
            else if (mouseMsg == WmLButtonUp)
            {
                // Sol tek tık: Çift tık ihtimaline karşı 220 ms bekle
                _singleClickTimer?.Stop();
                _singleClickTimer?.Start();
                handled = true;
            }
            else if (mouseMsg == WmRButtonUp)
            {
                // Sağ tık: Beklemeden doğrudan açılır menüyü aç
                _singleClickTimer?.Stop();
                SagAltMenuWindow.Toggle();
                handled = true;
            }
        }
        return 0;
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;

        _singleClickTimer?.Stop();
        _singleClickTimer = null;

        _trayService?.Remove();
        _trayService?.Dispose();
        _trayService = null;

        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}

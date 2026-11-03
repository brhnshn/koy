using System.Management;
using System.Runtime.InteropServices;
using Koy.Core.Display;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace Koy.Platform.Display;

public sealed class BrightnessService : IBrightnessService
{
    private bool _isWmi;

    public bool IsSupported { get; private set; }
    public int Brightness { get; private set; } = 50;

    public event EventHandler<int>? BrightnessChanged;

    public BrightnessService()
    {
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            // 1. Önce dahili ekran WMI kontrolü
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentBrightness FROM WmiMonitorBrightness WHERE Active = TRUE");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                var val = mo["CurrentBrightness"];
                if (val is not null)
                {
                    Brightness = Convert.ToInt32(val);
                    IsSupported = true;
                    _isWmi = true;
                    return;
                }
            }
        }
        catch
        {
            // WMI desteklenmiyor veya masaüstü PC
        }

        // 2. WMI bulunamadıysa harici ekran DDC/CI kontrolü
        try
        {
            var hMon = PInvoke.MonitorFromWindow(Windows.Win32.Foundation.HWND.Null, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
            if (hMon != 0)
            {
                if (PInvoke.GetNumberOfPhysicalMonitorsFromHMONITOR(hMon, out var count) && count > 0)
                {
                    var monitors = new Windows.Win32.Devices.Display.PHYSICAL_MONITOR[count];
                    unsafe
                    {
                        fixed (Windows.Win32.Devices.Display.PHYSICAL_MONITOR* pMonitors = monitors)
                        {
                            if (PInvoke.GetPhysicalMonitorsFromHMONITOR(hMon, (uint)count, pMonitors))
                            {
                                using var safeHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(monitors[0].hPhysicalMonitor.Value, ownsHandle: false);
                                if (PInvoke.GetMonitorBrightness(safeHandle, out var min, out var current, out var max) != 0)
                                {
                                    Brightness = (int)current;
                                    IsSupported = true;
                                    _isWmi = false;
                                }
                                PInvoke.DestroyPhysicalMonitors((uint)count, pMonitors);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // DDC/CI desteklenmiyor
        }
    }

    public void SetBrightness(int brightness)
    {
        if (!IsSupported)
            return;

        var clamped = Math.Clamp(brightness, 0, 100);

        if (_isWmi)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorBrightnessMethods WHERE Active = TRUE");
                using var results = searcher.Get();
                foreach (ManagementObject mo in results)
                {
                    mo.InvokeMethod("WmiSetBrightness", [1, (byte)clamped]);
                    Brightness = clamped;
                    BrightnessChanged?.Invoke(this, Brightness);
                    break;
                }
            }
            catch
            {
            }
        }
        else
        {
            try
            {
                var hMon = PInvoke.MonitorFromWindow(Windows.Win32.Foundation.HWND.Null, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
                if (hMon != 0 && PInvoke.GetNumberOfPhysicalMonitorsFromHMONITOR(hMon, out var count) && count > 0)
                {
                    var monitors = new Windows.Win32.Devices.Display.PHYSICAL_MONITOR[count];
                    unsafe
                    {
                        fixed (Windows.Win32.Devices.Display.PHYSICAL_MONITOR* pMonitors = monitors)
                        {
                            if (PInvoke.GetPhysicalMonitorsFromHMONITOR(hMon, (uint)count, pMonitors))
                            {
                                using var safeHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(monitors[0].hPhysicalMonitor.Value, ownsHandle: false);
                                if (PInvoke.SetMonitorBrightness(safeHandle, (uint)clamped) != 0)
                                {
                                    Brightness = clamped;
                                    BrightnessChanged?.Invoke(this, Brightness);
                                }
                                PInvoke.DestroyPhysicalMonitors((uint)count, pMonitors);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }

    public void Dispose()
    {
    }
}

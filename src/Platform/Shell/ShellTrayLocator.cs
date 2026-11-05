using Windows.Win32;
using Windows.Win32.Foundation;

namespace Koy.Platform.Shell;

/// <summary>
/// Windows Gezgini'nin görev çubuğu penceresini bulur.
/// Koy kendi tepsisini açınca aynı sınıf adlı (Shell_TrayWnd) ikinci bir pencere olur ve FindWindow onu
/// döndürebilir; görev çubuğunu gizleyip geri getiren kod yanlış pencereye dokunmasın diye Koy'un pencereleri atlanır.
/// </summary>
internal static class ShellTrayLocator
{
    public const string TrayClassName = "Shell_TrayWnd";

    public static unsafe HWND FindExplorerTray()
    {
        var ownProcessId = (uint)Environment.ProcessId;
        var hwnd = HWND.Null;
        while (!(hwnd = PInvoke.FindWindowEx(HWND.Null, hwnd, TrayClassName, null)).IsNull)
        {
            uint processId = 0;
            PInvoke.GetWindowThreadProcessId(hwnd, &processId);
            if (processId != ownProcessId)
                return hwnd;
        }

        return HWND.Null;
    }
}

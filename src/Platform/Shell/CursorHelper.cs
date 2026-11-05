using Windows.Win32;

namespace Koy.Platform.Shell;

/// <summary>
/// İmleç (fare) konumunu Win32 GetCursorPos API'si ile sağlayan yardımcı sınıf.
/// </summary>
public static class CursorHelper
{
    public static bool GetPosition(out int x, out int y)
    {
        if (PInvoke.GetCursorPos(out var pt))
        {
            x = pt.X;
            y = pt.Y;
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    /// <summary>Kullanıcının Windows'ta ayarladığı çift tıklama süresi (ms).</summary>
    public static uint DoubleClickTimeMs => PInvoke.GetDoubleClickTime();
}

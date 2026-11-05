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

    /// <summary>
    /// Sol, sağ ya da orta fare tuşu şu an basılı mı (pencere sürüklerken kenara inince dock açılmasın).
    /// Yoklama tabanlıdır; kanca kurulmaz.
    /// </summary>
    public static bool IsAnyMouseButtonDown()
    {
        const int VK_LBUTTON = 0x01, VK_RBUTTON = 0x02, VK_MBUTTON = 0x04;
        return IsDown(VK_LBUTTON) || IsDown(VK_RBUTTON) || IsDown(VK_MBUTTON);

        static bool IsDown(int vk) => (PInvoke.GetAsyncKeyState(vk) & 0x8000) != 0;
    }

    /// <summary>Kullanıcının Windows'ta ayarladığı çift tıklama süresi (ms).</summary>
    public static uint DoubleClickTimeMs => PInvoke.GetDoubleClickTime();
}

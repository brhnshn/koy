using System.Diagnostics;
using Koy.Core.Shell;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Koy.Platform.Shell;

/// <summary>
/// Başlat menüsü, sistem tepsisi kısayolları ve uygulama başlatma işlemlerini yöneten servis.
/// </summary>
public sealed class ShellActionService : IShellActionService
{
    public unsafe void SendWinKey()
    {
        var inputs = new INPUT[2];

        // Win tuşu basıldı
        inputs[0].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[0].Anonymous.ki.wVk = VIRTUAL_KEY.VK_LWIN;

        // Win tuşu bırakıldı
        inputs[1].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[1].Anonymous.ki.wVk = VIRTUAL_KEY.VK_LWIN;
        inputs[1].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;

        PInvoke.SendInput(inputs, sizeof(INPUT));
    }

    public unsafe void ToggleSystemTray()
    {
        // Win + B kısayolu sistem tepsisi taşma simgesini odaklar
        var inputs = new INPUT[4];

        // Win basıldı
        inputs[0].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[0].Anonymous.ki.wVk = VIRTUAL_KEY.VK_LWIN;

        // B basıldı
        inputs[1].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[1].Anonymous.ki.wVk = VIRTUAL_KEY.VK_B;

        // B bırakıldı
        inputs[2].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[2].Anonymous.ki.wVk = VIRTUAL_KEY.VK_B;
        inputs[2].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;

        // Win bırakıldı
        inputs[3].type = INPUT_TYPE.INPUT_KEYBOARD;
        inputs[3].Anonymous.ki.wVk = VIRTUAL_KEY.VK_LWIN;
        inputs[3].Anonymous.ki.dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;

        PInvoke.SendInput(inputs, sizeof(INPUT));
    }

    public void Launch(string path, string? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };

            if (!string.IsNullOrEmpty(arguments))
                psi.Arguments = arguments;

            Process.Start(psi);
        }
        catch
        {
            // Kullanıcı iptali veya yetki hatası yutulur
        }
    }
}

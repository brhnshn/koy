using System.Diagnostics;
using System.IO;
using Koy.Core.Shell;

namespace Koy.Platform.Shell;

// Koy'a özel kısım: görev çubuğu gizlenince gözcü süreci başlatılır, temiz geri yüklemede durdurulur.
// Bu dosya Koy.Watchdog projesine bağlanmaz (orada kısmi yöntemler boştur).
public static partial class TaskbarService
{
    static partial void OnHidden() => WatchdogLauncher.EnsureRunning();

    static partial void OnRestoring() => WatchdogLauncher.Stop();
}

/// <summary>
/// Koy.Watchdog.exe'yi yönetir: Koy görev çubuğunu gizlediği sürece tek bir gözcü çalışır. Görev Yöneticisi
/// TerminateProcess ile Koy'u öldürünce hiçbir Koy kodu çalışmaz; gözcü sürecin kapandığını görüp görev çubuğunu
/// geri getirir. Gözcü exe'si yoksa (geliştirme çalıştırması) sessizce atlanır.
/// </summary>
internal static class WatchdogLauncher
{
    public const string ExeName = "Koy.Watchdog.exe";

    private static readonly object Gate = new();
    private static Process? _process;
    private static EventWaitHandle? _stopEvent;

    public static void EnsureRunning()
    {
        lock (Gate)
        {
            try
            {
                if (_process is { HasExited: false })
                    return;

                var path = Path.Combine(AppContext.BaseDirectory, ExeName);
                if (!File.Exists(path))
                    return;

                ReleaseHandles();

                // Olay gözcüden önce kurulur ve Koy tutar: gözcü açtığında temiz çıkış işareti kaçmaz.
                _stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset,
                    WatchdogPolicy.StopEventName(Environment.ProcessId));
                _stopEvent.Reset();

                _process = Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = WatchdogPolicy.BuildArguments(Environment.ProcessId),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch
            {
                // Gözcü başlatılamazsa dock çalışmaya devam eder; açılıştaki Restore() korumaları yerinde.
                ReleaseHandles();
            }
        }
    }

    /// <summary>Temiz çıkış: gözcü görev çubuğuna dokunmadan kapanır.</summary>
    public static void Stop()
    {
        lock (Gate)
        {
            try
            {
                if (_process is null)
                    return;

                _stopEvent?.Set();

                // Kısa bekleme: gözcü olayı alıp çıksın ki Koy'un kapanışında yetim kalmasın.
                _process.WaitForExit(300);
            }
            catch
            {
                // Gözcü zaten kapanmış olabilir.
            }
            finally
            {
                ReleaseHandles();
            }
        }
    }

    private static void ReleaseHandles()
    {
        _process?.Dispose();
        _process = null;
        _stopEvent?.Dispose();
        _stopEvent = null;
    }
}

using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Koy.App.Features;
using Koy.App.Features.Dock;
using Koy.App.Features.Menu;
using Koy.App.Features.Notch;
using Koy.App.Features.Settings;
using Koy.App.Features.Wizard;
using Koy.Core.Diagnostics;
using Koy.Core.Settings;
using Koy.Platform.Shell;
using Velopack;

namespace Koy.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack kurulum ve güncelleme kancaları: kaldırma ve güncelleme sürecin ölmesiyle biter,
        // OnExit çalışmayabilir; görev çubuğu bu kancalarda mutlaka geri getirilir (hata verse bile süreç düşmesin).
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => App.SafeRestoreTaskbar())
            .OnBeforeUpdateFastCallback(_ => App.SafeRestoreTaskbar())
            .OnAfterUpdateFastCallback(_ => App.SafeRestoreTaskbar())
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}

public partial class App : Application
{
    private readonly List<IFeature> _features = [];
    private TrayIconManager? _trayIconManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Bellek: GPU ile çizimde sürücü sürece ~135 MB ekliyor (ölçüm: 169 MB → 34 MB private, RTX 4050).
        // Yüzeyler küçük olduğu için yazılımla çizim yeterli. Karşılaştırma için KOY_GPU=1.
        if (Environment.GetEnvironmentVariable("KOY_GPU") != "1")
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        // Görev çubuğu güvenliği: önceki oturum çöktüyse görev çubuğunu geri getir.
        SafeRestoreTaskbar();
        InstallGlobalErrorHandlers();

        var store = new SettingsStore(SettingsStore.DefaultPath);
        var settings = store.Load();

        // İlk açılış sihirbazı: Ayar henüz tamamlanmadıysa 3 adımlı sihirbazı başlat
        bool isFirstRun = !settings.FirstRunCompleted;
        if (isFirstRun)
        {
            var wizard = new SetupWizardWindow();
            wizard.ShowDialog();
            settings = store.Load();
        }

        // Sihirbaz X ile kapatıldıysa (tamamlanmadıysa) dock açılıp görev çubuğu gizlenmesin.
        settings = FirstRunPolicy.ApplySafeDefaultsIfIncomplete(settings);

        var culture = new System.Globalization.CultureInfo(settings.Language == "en" ? "en-US" : "tr-TR");
        System.Globalization.CultureInfo.CurrentUICulture = culture;
        System.Globalization.CultureInfo.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        // Dock Koy logosu tıklandığında Sağ Alt Menüyü aç
        DockViewModel.OpenKoyMenuHandler = () => SagAltMenuWindow.Toggle();

        // Sistem tepsisi simgesi: dock kapalıyken de Koy menüsüne erişilmesini sağlar
        _trayIconManager = new TrayIconManager();
        _trayIconManager.ExplorerRestarted += OnExplorerRestarted;
        _trayIconManager.Start();

        // Yalnız açık özellikler başlatılır. Dock kapalıyken görev çubuğuna hiç dokunulmaz.
        if (settings.NotchEnabled)
            _features.Add(new NotchFeature(settings));

        if (settings.DockEnabled)
            _features.Add(new DockFeature(settings));

        foreach (var feature in _features)
            feature.Start();

        // İlk kurulumdan hemen sonra ana uygulama doğrudan ekrana çıksın
        if (isFirstRun)
        {
            KoyWindow.ShowInstance();
        }
    }

    /// <summary>
    /// Görev çubuğunu geri getirir; hata verse bile çağıranın (çökme işleyicisi) akışını bozmaz.
    /// </summary>
    private void OnExplorerRestarted()
    {
        CrashLog.Guard("Explorer yeniden başladı", () =>
        {
            foreach (var dock in _features.OfType<DockFeature>())
                dock.ReapplyTaskbarHiding();
        });
    }

    internal static void SafeRestoreTaskbar()
    {
        try
        {
            TaskbarService.Restore();
        }
        catch (Exception ex)
        {
            CrashLog.Write("TaskbarService.Restore başarısız", ex);
        }
    }

    private void InstallGlobalErrorHandlers()
    {
        // Önce log, sonra görev çubuğu: Restore bir istisna atsa bile kayıt yazılmış olur.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            CrashLog.Write(args.IsTerminating ? "AppDomain Unhandled (süreç sonlanıyor)" : "AppDomain Unhandled", args.ExceptionObject);
            SafeRestoreTaskbar();
        };

        DispatcherUnhandledException += (_, args) =>
        {
            CrashLog.Write("Dispatcher Unhandled", args.Exception);
            SafeRestoreTaskbar();
        };

        // Gözlenmeyen Task hataları: süreci düşürmez, ama izi kalsın.
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            CrashLog.Write("Unobserved Task", args.Exception);
            args.SetObserved();
        };
    }

    public void UpdateNotchState(bool enabled)
    {
        var existingNotch = _features.OfType<NotchFeature>().FirstOrDefault();
        if (enabled && existingNotch is null)
        {
            var settings = new SettingsStore(SettingsStore.DefaultPath).Load();
            var notch = new NotchFeature(settings);
            _features.Add(notch);
            notch.Start();
        }
        else if (!enabled && existingNotch is not null)
        {
            existingNotch.Stop();
            _features.Remove(existingNotch);
        }
    }

    public void UpdateDockState(bool enabled)
    {
        var existingDock = _features.OfType<DockFeature>().FirstOrDefault();
        if (enabled && existingDock is null)
        {
            var settings = new SettingsStore(SettingsStore.DefaultPath).Load();
            var dock = new DockFeature(settings);
            _features.Add(dock);
            dock.Start();
        }
        else if (!enabled && existingDock is not null)
        {
            existingDock.Stop();
            _features.Remove(existingDock);
            TaskbarService.Restore();
        }
    }

    public void ReloadDock()
    {
        var existingDock = _features.OfType<DockFeature>().FirstOrDefault();
        if (existingDock is not null)
        {
            existingDock.Stop();
            _features.Remove(existingDock);
            var settings = new SettingsStore(SettingsStore.DefaultPath).Load();
            if (settings.DockEnabled)
            {
                var dock = new DockFeature(settings);
                _features.Add(dock);
                dock.Start();
            }
        }
    }

    public void ReloadNotch()
    {
        var existingNotch = _features.OfType<NotchFeature>().FirstOrDefault();
        if (existingNotch is not null)
        {
            existingNotch.Stop();
            _features.Remove(existingNotch);
            var settings = new SettingsStore(SettingsStore.DefaultPath).Load();
            if (settings.NotchEnabled)
            {
                var notch = new NotchFeature(settings);
                _features.Add(notch);
                notch.Start();
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconManager?.Dispose();
        _trayIconManager = null;

        foreach (var feature in _features)
            feature.Stop();

        TaskbarService.Restore();
        base.OnExit(e);
    }
}

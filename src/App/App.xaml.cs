using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Koy.App.Features;
using Koy.App.Features.Dock;
using Koy.App.Features.Notch;
using Koy.Core.Settings;
using Koy.Platform.Shell;

namespace Koy.App;

public partial class App : Application
{
    private readonly List<IFeature> _features = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Bellek: GPU ile çizimde sürücü sürece ~135 MB ekliyor (ölçüm: 169 MB → 34 MB private, RTX 4050).
        // Yüzeyler küçük olduğu için yazılımla çizim yeterli. Karşılaştırma için KOY_GPU=1.
        if (Environment.GetEnvironmentVariable("KOY_GPU") != "1")
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        // Görev çubuğu güvenliği: önceki oturum çöktüyse görev çubuğunu geri getir.
        TaskbarService.Restore();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => TaskbarService.Restore();
        DispatcherUnhandledException += (_, _) => TaskbarService.Restore();

        var settings = new SettingsStore(SettingsStore.DefaultPath).Load();

        var culture = new System.Globalization.CultureInfo(settings.Language == "en" ? "en-US" : "tr-TR");
        System.Globalization.CultureInfo.CurrentUICulture = culture;
        System.Globalization.CultureInfo.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        // Yalnız açık özellikler başlatılır. Dock kapalıyken görev çubuğuna hiç dokunulmaz.
        if (settings.NotchEnabled)
            _features.Add(new NotchFeature(settings));

        if (settings.DockEnabled)
            _features.Add(new DockFeature(settings));

        foreach (var feature in _features)
            feature.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        foreach (var feature in _features)
            feature.Stop();
        TaskbarService.Restore();
        base.OnExit(e);
    }
}

using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Koy.App.Features.Menu;
using Koy.App.Features.Settings;
using Koy.App.Features.Wizard;
using Koy.Core.Settings;

namespace Koy.UiSnapshots;

/// <summary>
/// Koy'un pencerelerini ekrana çıkarmadan PNG'ye çevirir. Hiçbir Win32 kancası, AppBar ya da görev çubuğu
/// servisi başlatılmaz; pencereler gösterilmez, yalnız ölçülüp çizilir.
/// Kullanım: Koy.UiSnapshots --out &lt;klasör&gt; --prefix once|sonra [--theme dark|light|both] [--scene all|uninstall]
/// <c>--scene uninstall</c>: yalnız Hakkında sayfası ve kaldırma onay görünümü (kaldırıcı hiçbir zaman çalıştırılmaz).
/// </summary>
public static class Program
{
    private const double Scale = 1.5;

    private static readonly string[] PageNames =
        ["genel", "centik", "dock", "ekranlar", "gorunum", "kisayollar", "performans", "hakkinda"];

    [STAThread]
    public static int Main(string[] args)
    {
        var outDir = Arg(args, "--out") ?? Path.Combine(Environment.CurrentDirectory, "ui-snapshots");
        var prefix = Arg(args, "--prefix") ?? "snap";
        var themeArg = (Arg(args, "--theme") ?? "dark").ToLowerInvariant();
        var scene = (Arg(args, "--scene") ?? "all").ToLowerInvariant();
        Directory.CreateDirectory(outDir);

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SnapshotTheme.Install(app);

        var themes = themeArg == "both" ? new[] { "dark", "light" } : new[] { themeArg };
        foreach (var theme in themes)
        {
            SnapshotTheme.Apply(app, theme);
            if (scene == "uninstall")
                RenderUninstall(outDir, prefix, theme);
            else if (scene == "centik")
                RenderCentik(outDir, prefix, theme);
            else
                RenderAll(outDir, prefix, theme);
        }

        Console.WriteLine($"Bitti: {outDir}");
        return 0;
    }

    private static void RenderAll(string outDir, string prefix, string theme)
    {
        // Ayarlar geçici bir dosyada: gerçek %AppData%\Koy\settings.json'a dokunulmaz.
        var tempSettings = Path.Combine(Path.GetTempPath(), "koy-uisnap-" + Guid.NewGuid().ToString("N") + ".json");
        var store = new SettingsStore(tempSettings);
        store.Save(new AppSettings { FirstRunCompleted = true });

        try
        {
            var vm = new KoyWindowViewModel(store);
            var win = new KoyWindow(vm);
            // Sayfanın tamamı görünsün diye kaydırma kapatılır, pencere içeriğe göre uzar.
            if (win.FindName("ContentScrollViewer") is System.Windows.Controls.ScrollViewer sv)
                sv.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled;

            // İlk ölçüm sayfa şablonlarını kurar; ardından her sayfa gerçek boyutuyla çizilir.
            ((FrameworkElement)win.Content).Measure(new Size(880, double.PositiveInfinity));
            for (var i = 0; i < PageNames.Length; i++)
            {
                vm.SelectPage(i);
                RenderElement((FrameworkElement)win.Content, 880, 680, Path.Combine(outDir, $"{prefix}-{theme}-koy-{PageNames[i]}.png"));
            }

            var wizardVm = new SetupWizardViewModel(store);
            var wizard = new SetupWizardWindow(wizardVm);
            for (var step = 1; step <= SetupWizardViewModel.LastStep; step++)
            {
                wizardVm.CurrentStep = step;
                RenderElement((FrameworkElement)wizard.Content, 880, 600, Path.Combine(outDir, $"{prefix}-{theme}-sihirbaz-{step}.png"));
            }

            var menu = new SagAltMenuWindow(new SagAltMenuViewModel(store));
            RenderElement((FrameworkElement)menu.Content, 320, 0, Path.Combine(outDir, $"{prefix}-{theme}-sagalt-menu.png"));
        }
        finally
        {
            try { File.Delete(tempSettings); } catch { }
        }
    }

    /// <summary>
    /// Çentik H arayüzü (Ana, Odak, Raf, Kontrol ve kapalı çentik) ve ayarlardaki hava durumu bölümü.
    /// Çentik gerçek NotchWindow ve NotchViewModel ile kurulur ama gösterilmez; medya, pil, hava, ses ve radyo
    /// servisleri sahte (CentikFakes.cs), zamanlayıcılar gerçek. Gerçek veriye ve görev çubuğuna dokunulmaz.
    /// </summary>
    private static void RenderCentik(string outDir, string prefix, string theme)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "koy-uisnap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var store = new SettingsStore(Path.Combine(tempDir, "settings.json"));
        store.Save(new AppSettings { FirstRunCompleted = true });

        try
        {
            string Name(string n) => Path.Combine(outDir, $"{prefix}-{theme}-{n}.png");
            RenderNotchScenes(Name);
            // Ayarlar › Çentik › Hava durumu (örnek arama sonuçlarıyla)
            var kvm = new KoyWindowViewModel(store);
            var win = new KoyWindow(kvm);
            if (win.FindName("ContentScrollViewer") is System.Windows.Controls.ScrollViewer sv)
                sv.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled;
            var root = (FrameworkElement)win.Content;
            root.Measure(new Size(880, double.PositiveInfinity));
            kvm.SelectPage(1);
            RenderElement(root, 880, 680, Name("ayar-centik-hava-bos"));

            kvm.Weather.Query = "Ankara";
            kvm.Weather.Results.Add(new Koy.Core.Weather.GeoPlace("Ankara", "Ankara", "Türkiye", 39.92, 32.85));
            kvm.Weather.Results.Add(new Koy.Core.Weather.GeoPlace("Ankara", null, "Gabon", -1.5, 10.5));
            RenderElement(root, 880, 680, Name("ayar-centik-hava-sonuc"));

            kvm.Weather.SelectPlaceCommand.Execute(kvm.Weather.Results[0]);
            RenderElement(root, 880, 680, Name("ayar-centik-hava-secili"));

            kvm.SelectPage(7);
            RenderElement(root, 880, 680, Name("ayar-hakkinda-atif"));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    private static void Flush() =>
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);

    private sealed record NotchSet(
        Koy.App.Features.Notch.NotchWindow Window,
        Koy.App.Features.Notch.NotchViewModel Vm,
        Koy.Core.Timers.FocusTimers Timers);

    private static NotchSet MakeNotch(bool music, IReadOnlyList<Koy.Core.Shelf.ShelfItem>? shelf = null, List<string>? tileOrder = null)
    {
        var settings = new AppSettings
        {
            FirstRunCompleted = true,
            WeatherLocationSource = Koy.Core.Weather.WeatherLocationSource.City,
            WeatherCity = "İstanbul, Türkiye",
            WeatherLatitude = 41.01,
            WeatherLongitude = 28.97,
            ControlCenterLayout = tileOrder ?? new AppSettings().ControlCenterLayout
        };

        var controller = new Koy.Core.Notch.NotchController();
        controller.SetEnabledPanels(Koy.Core.Notch.NotchPanels.Order);
        var timers = new Koy.Core.Timers.FocusTimers(Koy.Core.Timers.PomodoroConfig.Default);
        var info = music ? new Koy.Core.Media.MediaInfo("Gece Yolculuğu", "Mor ve Ötesi", "Spotify.exe", true, null) : null;

        var vm = new Koy.App.Features.Notch.NotchViewModel(
            settings,
            controller,
            new Koy.App.Features.Music.MusicViewModel(new FakeMedia(info)),
            new Koy.App.Features.ControlCenter.ControlCenterViewModel(
                () => new FakeAudio(), () => new FakeRadio(), null, () => new FakeBrightness(), new FakeFocusAssist(), settings),
            new Koy.App.Features.Status.StatusViewModel(new FakePower(), new FakeWeather(), settings),
            new Koy.App.Features.Focus.FocusViewModel(timers, settings),
            new Koy.App.Features.Shelf.ShelfViewModel(new FakeShelf(shelf ?? [])));

        var window = new Koy.App.Features.Notch.NotchWindow { DataContext = vm };
        return new NotchSet(window, vm, timers);
    }

    private static void ShotNotch(NotchSet set, string path, bool expanded)
    {
        var shape = (Koy.App.Features.Notch.NotchShape)set.Window.FindName("Notch");
        if (expanded)
        {
            shape.Width = Koy.Core.Notch.NotchLayout.ExpandedWidth;
            shape.Height = Koy.Core.Notch.NotchLayout.ExpandedHeight;
            shape.CornerRadius = 34;
        }
        else
        {
            shape.Width = set.Vm.IsMusicPlaying ? 280 : 236;
            shape.Height = 34;
            shape.CornerRadius = 18;
        }

        Flush();
        RenderElement((FrameworkElement)set.Window.Content, Koy.Core.Notch.NotchLayout.CanvasWidth, Koy.Core.Notch.NotchLayout.CanvasHeight, path);
    }

    private static void RenderNotchScenes(Func<string, string> name)
    {
        var shelfItems = new[]
        {
            new Koy.Core.Shelf.ShelfItem("1", "sunum.pdf", @"C:\Belgeler\sunum.pdf", Koy.Core.Shelf.ShelfItemKind.File, DateTimeOffset.Now),
            new Koy.Core.Shelf.ShelfItem("2", "ekran.png", @"C:\Belgeler\ekran.png", Koy.Core.Shelf.ShelfItemKind.File, DateTimeOffset.Now),
            new Koy.Core.Shelf.ShelfItem("3", "notlar", @"C:\Belgeler\notlar", Koy.Core.Shelf.ShelfItemKind.Directory, DateTimeOffset.Now),
            new Koy.Core.Shelf.ShelfItem("4", "Kopyalanan metin", "Kopyalanan metin", Koy.Core.Shelf.ShelfItemKind.Text, DateTimeOffset.Now)
        };

        // Kapalı çentik: boşta, müzik çalarken, zamanlayıcı çalışırken.
        var set = MakeNotch(music: false);
        ShotNotch(set, name("kapali"), expanded: false);
        set.Timers.Toggle(Koy.Core.Timers.FocusKind.Focus);
        Flush();
        ShotNotch(set, name("kapali-zamanlayici"), expanded: false);
        set.Vm.Dispose();

        set = MakeNotch(music: true);
        ShotNotch(set, name("kapali-muzik"), expanded: false);
        set.Vm.Dispose();

        // Ana: müzik çalıyor / çalmıyor + çalışan zamanlayıcı satırı.
        set = MakeNotch(music: true);
        set.Vm.Controller.Expand(Koy.Core.Notch.NotchPanel.Home);
        ShotNotch(set, name("ana-muzik"), expanded: true);
        set.Vm.Dispose();

        set = MakeNotch(music: false);
        set.Vm.Controller.Expand(Koy.Core.Notch.NotchPanel.Home);
        ShotNotch(set, name("ana-bos"), expanded: true);
        set.Timers.Toggle(Koy.Core.Timers.FocusKind.Focus);
        Thread.Sleep(1100);
        Flush();
        ShotNotch(set, name("ana-bos-zamanlayici"), expanded: true);

        // Odak: Odak (çalışıyor), Mola, Sayaç, Kronometre (turlarla).
        set.Vm.Controller.GoToPanel(Koy.Core.Notch.NotchPanel.Focus);
        ShotNotch(set, name("odak"), expanded: true);
        set.Vm.Focus.SelectTabCommand.Execute(Koy.Core.Timers.FocusKind.Break);
        ShotNotch(set, name("odak-mola"), expanded: true);
        set.Vm.Focus.SelectTabCommand.Execute(Koy.Core.Timers.FocusKind.Countdown);
        ShotNotch(set, name("odak-sayac"), expanded: true);
        set.Vm.Focus.SelectTabCommand.Execute(Koy.Core.Timers.FocusKind.Stopwatch);
        set.Vm.Focus.ToggleCommand.Execute(null);
        Thread.Sleep(1300);
        set.Vm.Focus.LapCommand.Execute(null);
        Thread.Sleep(700);
        set.Vm.Focus.LapCommand.Execute(null);
        Flush();
        ShotNotch(set, name("odak-kronometre"), expanded: true);
        set.Vm.Dispose();

        // Raf: dolu ve boş. Kontrol: varsayılan sıra ve kayıtlı özel sıra.
        set = MakeNotch(music: false, shelfItems);
        set.Vm.Controller.Expand(Koy.Core.Notch.NotchPanel.Shelf);
        ShotNotch(set, name("raf"), expanded: true);
        set.Vm.Dispose();

        set = MakeNotch(music: false);
        set.Vm.Controller.Expand(Koy.Core.Notch.NotchPanel.Shelf);
        ShotNotch(set, name("raf-bos"), expanded: true);
        set.Vm.Dispose();

        set = MakeNotch(music: false);
        set.Vm.Controller.Expand(Koy.Core.Notch.NotchPanel.ControlCenter);
        Flush();
        ShotNotch(set, name("kontrol"), expanded: true);
        set.Vm.Dispose();

        // Kayıtlı özel döşeme sırası korunuyor mu
        set = MakeNotch(music: false, tileOrder: ["donotdisturb", "wifi", "bluetooth"]);
        set.Vm.Controller.Expand(Koy.Core.Notch.NotchPanel.ControlCenter);
        Flush();
        ShotNotch(set, name("kontrol-ozel-sira"), expanded: true);
        set.Vm.Dispose();
    }
    /// <summary>Hakkında sayfası (kaldırma düğmesi kapalı ve açık) ve onay görünümü (ayarlar saklanır / silinir).</summary>
    private static void RenderUninstall(string outDir, string prefix, string theme)
    {
        var tempSettings = Path.Combine(Path.GetTempPath(), "koy-uisnap-" + Guid.NewGuid().ToString("N") + ".json");
        var store = new SettingsStore(tempSettings);
        store.Save(new AppSettings { FirstRunCompleted = true });

        try
        {
            var vm = new KoyWindowViewModel(store);
            var win = new KoyWindow(vm);
            if (win.FindName("ContentScrollViewer") is System.Windows.Controls.ScrollViewer sv)
                sv.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled;
            var root = (FrameworkElement)win.Content;
            root.Measure(new Size(880, double.PositiveInfinity));
            vm.SelectPage(7);

            string Path_(string name) => Path.Combine(outDir, $"{prefix}-{theme}-{name}.png");

            // Geliştirme çalıştırması: kurulu değil, düğme kapalı.
            RenderElement(root, 880, 680, Path_("hakkinda-devre-disi"));

            // Kurulu sürüm gibi: düğme açık. Gerçek kaldırıcıya hiçbir bağ yok, yalnız görünüm.
            vm.UninstallAvailability = Koy.Core.Uninstall.UninstallAvailability.Available;
            RenderElement(root, 880, 680, Path_("hakkinda"));

            vm.IsUninstallConfirming = true;
            RenderElement(root, 880, 680, Path_("onay"));

            vm.KeepSettingsOnUninstall = false;
            RenderElement(root, 880, 680, Path_("onay-ayarlar-silinecek"));

            vm.UninstallError = Koy.App.Resources.Strings.Get("UninstallFailed");
            RenderElement(root, 880, 680, Path_("onay-hata"));
        }
        finally
        {
            try { File.Delete(tempSettings); } catch { }
        }
    }

    /// <summary>Elemanı ölçer, yerleştirir ve nötr bir zemin üstüne PNG olarak yazar.</summary>
    private static void RenderElement(FrameworkElement el, double width, double minHeight, string path)
    {
        el.Measure(new Size(width, double.PositiveInfinity));
        var height = Math.Max(minHeight, Math.Ceiling(el.DesiredSize.Height));
        el.Measure(new Size(width, height));
        el.Arrange(new Rect(0, 0, width, height));
        el.UpdateLayout();

        const double pad = 16;
        var pxW = (int)Math.Ceiling((width + pad * 2) * Scale);
        var pxH = (int)Math.Ceiling((height + pad * 2) * Scale);

        var shot = new RenderTargetBitmap((int)Math.Ceiling(width * Scale), (int)Math.Ceiling(height * Scale), 96 * Scale, 96 * Scale, PixelFormats.Pbgra32);
        shot.Render(el);

        // Köşeler saydam kalmasın diye orta gri zemin: hem koyu hem açık tema okunur.
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x7A, 0x80, 0x8C)), null, new Rect(0, 0, pxW, pxH));
            dc.DrawImage(shot, new Rect(pad * Scale, pad * Scale, shot.PixelWidth, shot.PixelHeight));
        }
        var final = new RenderTargetBitmap(pxW, pxH, 96, 96, PixelFormats.Pbgra32);
        final.Render(dv);

        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(final));
        using var fs = File.Create(path);
        enc.Save(fs);
        Console.WriteLine(path);
    }

    private static string? Arg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}

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
            for (var step = 1; step <= 3; step++)
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

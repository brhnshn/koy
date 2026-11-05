using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Koy.Core.Display;
using Koy.Platform.Shell;

namespace Koy.App.Features.Settings;

/// <summary>
/// "Ekranları tanımla": her monitörün ortasında 2 saniye büyük ekran numarası gösterir. Tıklamayı ve odağı geçirir
/// (arkadaki pencereler etkilenmez); süre dolunca kendini kapatır. Şemadaki numarayla aynı numarayı kullanır.
/// </summary>
public sealed class IdentifyOverlayWindow : Window
{
    private const double Size = 180;
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan Fade = TimeSpan.FromMilliseconds(150);
    private static readonly List<IdentifyOverlayWindow> Open = [];

    private readonly DisplayMonitor _monitor;
    private readonly DispatcherTimer _timer = new() { Interval = Lifetime };
    private nint _hwnd;

    private IdentifyOverlayWindow(DisplayMonitor monitor, int number)
    {
        _monitor = monitor;

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        Width = Size;
        Height = Size;
        Opacity = 0;

        Content = new Border
        {
            CornerRadius = new CornerRadius(28),
            Background = new SolidColorBrush(Color.FromArgb(0xE6, 0x18, 0x18, 0x1B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = number.ToString(System.Globalization.CultureInfo.CurrentCulture),
                FontSize = 110,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF5)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        DpiChanged += (_, _) => Place();
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            FadeOutAndClose();
        };
    }

    /// <summary>Bütün monitörlerde numaraları gösterir; önceki tanımlama açıksa kapatır.</summary>
    public static void ShowOn(IReadOnlyList<DisplayMonitor> monitors)
    {
        foreach (var old in Open.ToList())
            old.Close();

        var ordered = DisplaySchema.Order(monitors);
        for (var i = 0; i < ordered.Count; i++)
        {
            var window = new IdentifyOverlayWindow(ordered[i], i + 1);
            Open.Add(window);
            window.Show();
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowStyles.ApplyToolWindowAndNoActivate(_hwnd);
        WindowStyles.SetClickThrough(_hwnd, true);
        Place();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Place();
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, Fade));
        _timer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        Open.Remove(this);
        base.OnClosed(e);
    }

    private void FadeOutAndClose()
    {
        var fade = new DoubleAnimation(0, Fade);
        fade.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fade);
    }

    // Hedef ekranın kendi ölçeğiyle piksel konumu; başka DPI'daki ekrana taşınıyorsa önce yalnız taşınır, DpiChanged'de boyutlanır.
    private void Place()
    {
        if (_hwnd == 0)
            return;

        var sameDpi = Math.Abs(VisualTreeHelper.GetDpi(this).DpiScaleX - _monitor.DpiScaleX) < 0.01;
        WindowStyles.SetBounds(_hwnd, WindowPlacementMath.Center(_monitor, Size, Size), resize: sameDpi);
    }
}

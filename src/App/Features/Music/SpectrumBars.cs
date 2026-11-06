using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Koy.App.Features.Music;

/// <summary>
/// Yakalama iş parçacığından gelen çubuk düzeylerini arayüz iş parçacığına taşır. Kareler birikmez: arayüz bir
/// öncekini çizmeden gelen yenisi yalnız son değeri günceller (kuyrukta en çok bir çağrı).
/// </summary>
public sealed class SpectrumFeed
{
    private readonly Dispatcher _dispatcher;
    private readonly object _gate = new();
    private readonly float[] _pending;
    private bool _queued;

    public SpectrumFeed(int bandCount, Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _pending = new float[bandCount];
        Levels = new float[bandCount];
    }

    /// <summary>Arayüz iş parçacığında okunan son düzeyler (0..1).</summary>
    public float[] Levels { get; }

    /// <summary>Yeni düzeyler arayüze geçti (arayüz iş parçacığında).</summary>
    public event Action? Updated;

    /// <summary>Herhangi bir iş parçacığından çağrılır.</summary>
    public void Push(float[] levels)
    {
        lock (_gate)
        {
            Array.Copy(levels, _pending, Math.Min(levels.Length, _pending.Length));
            if (_queued)
                return;
            _queued = true;
        }

        _dispatcher.BeginInvoke(DispatcherPriority.Render, Apply);
    }

    /// <summary>Çubukları sıfırlar (yakalama durdu).</summary>
    public void Clear() => Push(new float[_pending.Length]);

    private void Apply()
    {
        lock (_gate)
        {
            Array.Copy(_pending, Levels, Levels.Length);
            _queued = false;
        }

        Updated?.Invoke();
    }
}

/// <summary>
/// Müzik kartındaki ses çubukları (tasarım H: 24 çubuk, 4 px genişlik, 3 px boşluk, en çok 26 px, alttan büyür).
/// Tek OnRender ile çizilir; yalnız <see cref="SpectrumFeed.Updated"/> gelince yeniden çizilir (ses yokken çizim yok).
/// </summary>
public sealed class SpectrumBars : FrameworkElement
{
    private const double MinBar = 3;

    /// <summary>Çubuk genişliği (küçük kart 4, büyük kart daha geniş).</summary>
    public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register(
        nameof(BarWidth), typeof(double), typeof(SpectrumBars),
        new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Çubuklar arası boşluk.</summary>
    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap), typeof(double), typeof(SpectrumBars),
        new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public double BarWidth
    {
        get => (double)GetValue(BarWidthProperty);
        set => SetValue(BarWidthProperty, value);
    }

    public double Gap
    {
        get => (double)GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    public static readonly DependencyProperty FeedProperty = DependencyProperty.Register(
        nameof(Feed), typeof(SpectrumFeed), typeof(SpectrumBars),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnFeedChanged));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(SpectrumBars),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public SpectrumFeed? Feed
    {
        get => (SpectrumFeed?)GetValue(FeedProperty);
        set => SetValue(FeedProperty, value);
    }

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public SpectrumBars()
    {
        // Görsel ağaçtan çıkınca olay aboneliği kalmasın (çentik pencereleri kapanıp açılabilir).
        Loaded += (_, _) => Subscribe(Feed);
        Unloaded += (_, _) => Unsubscribe(Feed);
    }

    private static void OnFeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bars = (SpectrumBars)d;
        bars.Unsubscribe(e.OldValue as SpectrumFeed);
        if (bars.IsLoaded)
            bars.Subscribe(e.NewValue as SpectrumFeed);
    }

    private void Subscribe(SpectrumFeed? feed)
    {
        if (feed is null)
            return;
        feed.Updated -= InvalidateVisual;
        feed.Updated += InvalidateVisual;
    }

    private void Unsubscribe(SpectrumFeed? feed)
    {
        if (feed is not null)
            feed.Updated -= InvalidateVisual;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var count = Feed?.Levels.Length ?? 0;
        return new Size(count > 0 ? count * BarWidth + (count - 1) * Gap : 0, 0);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var levels = Feed?.Levels;
        if (levels is null || levels.Length == 0)
            return;

        var height = ActualHeight;
        var barWidth = BarWidth;
        var gap = Gap;
        var radius = barWidth / 2;
        var total = levels.Length * barWidth + (levels.Length - 1) * gap;
        var x = Math.Max(0, (ActualWidth - total) / 2);
        var fill = Fill;
        for (var i = 0; i < levels.Length; i++, x += barWidth + gap)
        {
            var barHeight = Math.Max(MinBar, Math.Round(levels[i] * height));
            dc.DrawRoundedRectangle(fill, null, new Rect(x, height - barHeight, barWidth, barHeight), radius, radius);
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Koy.App.Features.Notch;

/// <summary>
/// Ekrana sıfır yapışık çentik şekli.
/// Üst sol ve sağda ekrana yumuşak geçiş sağlayan içbükey (concave) kulaklar,
/// altta ise yuvarlatılmış dışbükey (convex) köşeler çizer.
/// </summary>
public class NotchShape : ContentControl
{
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(double),
            typeof(NotchShape),
            new FrameworkPropertyMetadata(18.0, FrameworkPropertyMetadataOptions.AffectsRender, OnRadiusChanged));

    public static readonly DependencyProperty EarRadiusProperty =
        DependencyProperty.Register(
            nameof(EarRadius),
            typeof(double),
            typeof(NotchShape),
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender, OnRadiusChanged));

    private static void OnRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NotchShape shape)
            shape.UpdateGeometry(shape.ActualWidth, shape.ActualHeight);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double EarRadius
    {
        get => (double)GetValue(EarRadiusProperty);
        set => SetValue(EarRadiusProperty, value);
    }

    private StreamGeometry? _geometry;

    public NotchShape()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        ClipToBounds = true;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateGeometry(sizeInfo.NewSize.Width, sizeInfo.NewSize.Height);
    }

    private void UpdateGeometry(double width, double height)
    {
        if (double.IsNaN(width) || double.IsNaN(height) || double.IsInfinity(width) || double.IsInfinity(height) || width <= 0 || height <= 0)
        {
            _geometry = null;
            Clip = null;
            return;
        }

        var geo = BuildGeometry(width, height, CornerRadius, EarRadius);
        _geometry = geo;
        Clip = geo;
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_geometry is not null && Background is not null)
            dc.DrawGeometry(Background, null, _geometry);

        base.OnRender(dc);
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        if (_geometry is not null && _geometry.FillContains(hitTestParameters.HitPoint))
            return new PointHitTestResult(this, hitTestParameters.HitPoint);

        return null;
    }

    public static StreamGeometry BuildGeometry(double width, double height, double cornerRadius, double earRadius)
    {
        var geometry = new StreamGeometry();
        if (double.IsNaN(width) || double.IsNaN(height) || double.IsInfinity(width) || double.IsInfinity(height) || width <= 0 || height <= 0)
            return geometry;

        var ear = Math.Max(0, Math.Min(earRadius, Math.Min(width / 4, height / 2)));
        var bot = Math.Max(0, Math.Min(cornerRadius, Math.Min((width - 2 * ear) / 2, height / 2)));

        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(0, 0), isFilled: true, isClosed: true);

            // 1. Sol üst içbükey kulak (ekrandan aşağı yumuşak kavis)
            if (ear > 0.01)
                ctx.ArcTo(new Point(ear, ear), new Size(ear, ear), 0, false, SweepDirection.Clockwise, true, true);
            else
                ctx.LineTo(new Point(0, 0), true, true);

            // 2. Sol dikey kenar
            ctx.LineTo(new Point(ear, height - bot), true, true);

            // 3. Sol alt dışbükey yuvarlak köşe
            if (bot > 0.01)
                ctx.ArcTo(new Point(ear + bot, height), new Size(bot, bot), 0, false, SweepDirection.Counterclockwise, true, true);

            // 4. Alt yatay kenar
            ctx.LineTo(new Point(width - ear - bot, height), true, true);

            // 5. Sağ alt dışbükey yuvarlak köşe
            if (bot > 0.01)
                ctx.ArcTo(new Point(width - ear, height - bot), new Size(bot, bot), 0, false, SweepDirection.Counterclockwise, true, true);

            // 6. Sağ dikey kenar
            ctx.LineTo(new Point(width - ear, ear), true, true);

            // 7. Sağ üst içbükey kulak (ekrana doğru yumuşak kavis)
            if (ear > 0.01)
                ctx.ArcTo(new Point(width, 0), new Size(ear, ear), 0, false, SweepDirection.Clockwise, true, true);

            // 8. Üst kenar boyunca başlangıç noktasına (0, 0)
            ctx.LineTo(new Point(0, 0), true, true);
        }

        geometry.Freeze();
        return geometry;
    }
}

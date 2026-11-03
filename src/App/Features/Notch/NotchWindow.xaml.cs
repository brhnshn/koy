using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Koy.Core.Animation;

namespace Koy.App.Features.Notch;

public partial class NotchWindow : Window
{
    private const double CollapsedWidth = 200, CollapsedHeight = 34;
    private const double ExpandedWidth = 380, ExpandedHeight = 110;

    private readonly Spring _width = new(CollapsedWidth);
    private readonly Spring _height = new(CollapsedHeight);
    private TimeSpan _lastFrame;
    private bool _animating;

    public NotchWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PlaceAtTopCenter();
    }

    private void PlaceAtTopCenter()
    {
        // M5: çoklu monitör ve DPI için Platform tarafındaki monitör servisine taşınacak.
        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        Top = 0;
    }

    private void Pill_MouseEnter(object sender, MouseEventArgs e) => AnimateTo(ExpandedWidth, ExpandedHeight);

    private void Pill_MouseLeave(object sender, MouseEventArgs e) => AnimateTo(CollapsedWidth, CollapsedHeight);

    private void AnimateTo(double width, double height)
    {
        _width.Target = width;
        _height.Target = height;
        if (_animating)
            return;

        // Kareler yalnız animasyon sürerken dinlenir; boştayken CPU harcanmaz.
        _animating = true;
        _lastFrame = TimeSpan.Zero;
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = ((RenderingEventArgs)e).RenderingTime;
        var dt = _lastFrame == TimeSpan.Zero ? 1.0 / 60 : (now - _lastFrame).TotalSeconds;
        _lastFrame = now;
        if (dt <= 0)
            return;

        var moving = _width.Step(dt) | _height.Step(dt);
        Pill.Width = _width.Value;
        Pill.Height = _height.Value;

        if (moving)
            return;

        CompositionTarget.Rendering -= OnRendering;
        _animating = false;
    }
}

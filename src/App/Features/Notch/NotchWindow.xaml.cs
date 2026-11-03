using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Koy.Core.Animation;
using Koy.Platform.Shell;

namespace Koy.App.Features.Notch;

public partial class NotchWindow : Window
{
    private readonly Spring _width = new(200);
    private readonly Spring _height = new(34);
    private readonly Spring _translateY = new(0);
    private readonly Spring _panelTranslateX = new(0);
    private readonly Spring _panelOpacity = new(1);
    private TimeSpan _lastFrame;
    private bool _animating;
    private bool _isNotchVisible = true;
    private nint _hwnd;

    private NotchViewModel? ViewModel => DataContext as NotchViewModel;

    public NotchWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PlaceAtTopCenter();
        DataContextChanged += OnDataContextChanged;
    }

    private const int WmDisplayChange = 0x007E;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowStyles.ApplyToolWindowAndNoActivate(_hwnd);

        var source = HwndSource.FromHwnd(_hwnd);
        source?.AddHook(WndProc);

        PlaceAtTopCenter();
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmDisplayChange)
        {
            PlaceAtTopCenter();
        }
        return 0;
    }

    public void PlaceAtTopCenter()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - Width) / 2;
        Top = 0;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is NotchViewModel oldVm)
        {
            oldVm.TargetSizeChanged -= AnimateTo;
            oldVm.TargetVisibilityChanged -= AnimateVisibility;
            oldVm.PanelTransitionTriggered -= AnimatePanelTransition;
            oldVm.ShakeRequested -= OnShakeRequested;
        }

        if (e.NewValue is NotchViewModel newVm)
        {
            newVm.TargetSizeChanged += AnimateTo;
            newVm.TargetVisibilityChanged += AnimateVisibility;
            newVm.PanelTransitionTriggered += AnimatePanelTransition;
            newVm.ShakeRequested += OnShakeRequested;
            AnimateTo(newVm.TargetWidth, newVm.TargetHeight);
            if (!newVm.IsNotchVisible)
            {
                // Peek modunda başlangıçta çentik aniden kaybolmak yerine doğrudan gizli başlar
                _isNotchVisible = false;
                _translateY.SnapTo(-150);
                NotchTranslate.Y = -150;
                if (_hwnd != 0)
                    WindowStyles.SetClickThrough(_hwnd, true);
            }
            else
            {
                AnimateVisibility(newVm.IsNotchVisible);
            }
        }
    }

    private void Notch_MouseEnter(object sender, MouseEventArgs e)
    {
        ViewModel?.Controller.OnMouseEnter();
        ViewModel?.Visibility.SetMouseOver(true);
        ViewModel?.Visibility.SetMouseAtEdge(false);
    }

    private void Notch_MouseLeave(object sender, MouseEventArgs e)
    {
        ViewModel?.Controller.OnMouseLeave();
        ViewModel?.Visibility.SetMouseOver(false);
        ViewModel?.Visibility.SetMouseAtEdge(false);
    }

    private void Notch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep)
        {
            var button = FindParent<Button>(dep);
            var progress = FindParent<ProgressBar>(dep);
            var slider = FindParent<Slider>(dep);
            if (button is not null || progress is not null || slider is not null)
                return;
        }

        ViewModel?.Controller.OnClick();
    }

    private void Notch_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ViewModel is null)
            return;

        if (e.Delta < 0)
            ViewModel.NextPanelCommand.Execute(null);
        else if (e.Delta > 0)
            ViewModel.PreviousPanelCommand.Execute(null);
    }

    private void Notch_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
        {
            e.Effects = DragDropEffects.Copy;
            if (ViewModel is not null)
            {
                if (ViewModel.Controller.State == Core.Notch.NotchState.Collapsed)
                {
                    ViewModel.Controller.OnMouseEnter();
                    ViewModel.Controller.OnClick();
                }
                ViewModel.Controller.SelectPanel(Core.Notch.NotchPanel.Shelf);
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Notch_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Notch_Drop(object sender, DragEventArgs e)
    {
        if (ViewModel is null)
            return;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                ViewModel.Shelf.HandleDrop(files, null);
            }
        }
        else if (e.Data.GetDataPresent(DataFormats.Text))
        {
            if (e.Data.GetData(DataFormats.Text) is string text)
            {
                ViewModel.Shelf.HandleDrop(null, text);
            }
        }

        e.Handled = true;
    }

    private void AnimatePanelTransition(int direction)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(() => AnimatePanelTransition(direction));
            return;
        }

        _panelTranslateX.SnapTo(direction > 0 ? 35 : -35);
        _panelTranslateX.Target = 0;
        _panelOpacity.SnapTo(0.25);
        _panelOpacity.Target = 1.0;
        WakeAnimation();
    }

    private void OnShakeRequested()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(OnShakeRequested);
            return;
        }

        _translateY.SnapTo(8);
        _translateY.Target = 0;
        WakeAnimation();
    }

    private void AnimateVisibility(bool isVisible)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(() => AnimateVisibility(isVisible));
            return;
        }

        _isNotchVisible = isVisible;
        if (isVisible)
        {
            if (_hwnd != 0)
            {
                WindowStyles.SetClickThrough(_hwnd, false);
                WindowStyles.ReassertTopmost(_hwnd);
            }

            _translateY.Target = 0;
        }
        else
        {
            var hideOffset = Notch.ActualHeight > 0 ? -(Notch.ActualHeight + 20) : -150;
            _translateY.Target = hideOffset;
        }

        WakeAnimation();
    }

    private void AnimateTo(double width, double height)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(() => AnimateTo(width, height));
            return;
        }

        _width.Target = width;
        _height.Target = height;
        WakeAnimation();
    }

    private void WakeAnimation()
    {
        if (_animating)
            return;

        // Kareler yalnız animasyon sürerken dinlenir; boştayken CPU harcanmaz (AGENTS.md #6).
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

        var moving = _width.Step(dt) | _height.Step(dt) | _translateY.Step(dt) | _panelTranslateX.Step(dt) | _panelOpacity.Step(dt);
        Notch.Width = _width.Value;
        Notch.Height = _height.Value;
        NotchTranslate.Y = _translateY.Value;
        PanelTranslate.X = _panelTranslateX.Value;
        ExpandedView.Opacity = _panelOpacity.Value;

        if (moving)
            return;

        CompositionTarget.Rendering -= OnRendering;
        _animating = false;

        if (!_isNotchVisible && _hwnd != 0)
        {
            WindowStyles.SetClickThrough(_hwnd, true);
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = child;
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}

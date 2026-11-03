using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Koy.Platform.Shell;

namespace Koy.App.Features.Dock;

public partial class DockWindow : Window
{
    private DockViewModel? ViewModel => DataContext as DockViewModel;
    private bool _isRenderingHooked;
    private TimeSpan _lastRenderTime;

    public DockWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        WindowStyles.ApplyToolWindowAndNoActivate(hwnd);

        Reposition();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.AnimationNeeded += OnAnimationNeeded;
        }

        Reposition();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Reposition();
    }

    public void Reposition()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        Left = Math.Max(0, (screenWidth - ActualWidth) / 2);
        Top = Math.Max(0, screenHeight - ActualHeight - 4);
    }

    private void OnAnimationNeeded(object? sender, EventArgs e)
    {
        StartRendering();
    }

    private void StartRendering()
    {
        if (_isRenderingHooked)
            return;

        _isRenderingHooked = true;
        _lastRenderTime = TimeSpan.Zero;
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopRendering()
    {
        if (!_isRenderingHooked)
            return;

        _isRenderingHooked = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs args)
            return;

        if (_lastRenderTime == TimeSpan.Zero)
        {
            _lastRenderTime = args.RenderingTime;
            return;
        }

        var dt = (args.RenderingTime - _lastRenderTime).TotalSeconds;
        _lastRenderTime = args.RenderingTime;

        if (dt <= 0 || dt > 0.1)
            dt = 0.016;

        ViewModel?.UpdateSprings(dt);
    }

    private void OnItemLeftClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DockItemViewModel item })
        {
            item.ClickCommand.Execute(null);
        }
    }

    private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle &&
            sender is FrameworkElement { DataContext: DockItemViewModel item })
        {
            item.MiddleClickCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnItemRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.ContextMenu is not null)
        {
            element.ContextMenu.PlacementTarget = element;
            element.ContextMenu.IsOpen = true;
            e.Handled = true;
        }
    }

    private void OnItemMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DockItemViewModel item } &&
            ViewModel is not null)
        {
            var index = ViewModel.Items.IndexOf(item);
            if (index >= 0)
            {
                ViewModel.OnItemHover(index);
            }
        }
    }

    private void OnItemMouseLeave(object sender, MouseEventArgs e)
    {
        ViewModel?.OnHoverEnded();
    }

    protected override void OnClosed(EventArgs e)
    {
        StopRendering();
        if (ViewModel is not null)
        {
            ViewModel.AnimationNeeded -= OnAnimationNeeded;
        }
        base.OnClosed(e);
    }
}

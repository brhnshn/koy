using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Koy.Core.Dock;
using Koy.Core.Shell;
using Koy.Platform.Shell;

namespace Koy.App.Features.Dock.Preview;

/// <summary>
/// Canlı DWM pencere küçük resmini bu öğenin kapladığı alana çizer.
/// Küçük resim, öğenin bulunduğu üst düzey pencereye (önizleme Popup'ının HWND'si) kaydedilir:
/// DWM alt pencereyi hedef olarak kabul etmez, saydam (layered) Popup içinde alt HWND zaten çizilmez.
/// Kayıt yalnız öğe görünürken tutulur; gizlenince, kaldırılınca ya da pencere değişince bırakılır.
/// </summary>
public sealed class WindowThumbnailPresenter : FrameworkElement
{
    public static readonly DependencyProperty WindowHandleProperty =
        DependencyProperty.Register(
            nameof(WindowHandle),
            typeof(nint),
            typeof(WindowThumbnailPresenter),
            new PropertyMetadata(nint.Zero, OnWindowHandleChanged));

    private HwndSource? _source;
    private nint _thumbHandle;
    private WindowRect _lastBox;
    private WindowRect _lastDestination;

    public WindowThumbnailPresenter()
    {
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
        IsVisibleChanged += OnIsVisibleChanged;
        // Popup her açılışta penceresini yeniden kurabilir; kayıt yeni HWND'ye taşınmalı.
        PresentationSource.AddSourceChangedHandler(this, OnSourceChanged);
    }

    public nint WindowHandle
    {
        get => (nint)GetValue(WindowHandleProperty);
        set => SetValue(WindowHandleProperty, value);
    }

    private static void OnWindowHandleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowThumbnailPresenter presenter)
        {
            presenter.Detach();
            presenter.Attach();
        }
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
            Attach();
        else
            Detach();
    }

    private void OnSourceChanged(object sender, SourceChangedEventArgs e)
    {
        Detach();
        Attach();
    }

    private void Attach()
    {
        if (_thumbHandle != 0 || !IsLoaded || !IsVisible || WindowHandle == 0)
            return;

        if (PresentationSource.FromVisual(this) is not HwndSource { Handle: not 0 } source)
            return;

        _thumbHandle = DwmThumbnailHelper.Register(source.Handle, WindowHandle);
        if (_thumbHandle == 0)
            return;

        _source = source;
        _lastBox = WindowRect.Empty;
        _lastDestination = WindowRect.Empty;
        LayoutUpdated += OnLayoutUpdated;
        UpdateDestination();
    }

    private void Detach()
    {
        LayoutUpdated -= OnLayoutUpdated;

        if (_thumbHandle != 0)
        {
            DwmThumbnailHelper.Unregister(_thumbHandle);
            _thumbHandle = 0;
        }

        _source = null;
        _lastBox = WindowRect.Empty;
        _lastDestination = WindowRect.Empty;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateDestination();

    private void UpdateDestination()
    {
        if (_thumbHandle == 0 || _source?.RootVisual is not Visual root || _source.CompositionTarget is null)
            return;

        if (ActualWidth <= 0 || ActualHeight <= 0 || !root.IsAncestorOf(this))
            return;

        // Hedef dikdörtgen, pencerenin istemci alanında fiziksel piksel ister; WPF DIP verir.
        var toRoot = TransformToAncestor(root);
        var topLeft = toRoot.Transform(new Point(0, 0));
        var bottomRight = toRoot.Transform(new Point(ActualWidth, ActualHeight));
        var toDevice = _source.CompositionTarget.TransformToDevice;

        var box = new WindowRect(
            (int)Math.Round(topLeft.X * toDevice.M11),
            (int)Math.Round(topLeft.Y * toDevice.M22),
            (int)Math.Round(bottomRight.X * toDevice.M11),
            (int)Math.Round(bottomRight.Y * toDevice.M22));

        // LayoutUpdated sık gelir; yer değişmediyse DWM'e gidilmez.
        if (box == _lastBox)
            return;

        var (sourceWidth, sourceHeight) = DwmThumbnailHelper.QuerySourceSize(_thumbHandle);
        var destination = ThumbnailFit.Fit(sourceWidth, sourceHeight, box);
        if (destination == _lastDestination)
        {
            _lastBox = box;
            return;
        }

        // Kutu yalnız DWM güncellemesi başarılı olunca hatırlanır; başarısızsa sonraki düzen geçişi yeniden dener.
        if (DwmThumbnailHelper.Update(_thumbHandle, destination))
        {
            _lastDestination = destination;
            _lastBox = box;
        }
    }
}

using System.Windows;
using System.Windows.Controls;

namespace Koy.App.Features.Dock.Preview;

/// <summary>
/// MVVM veri bağlama ile canlı DWM pencere küçük resmini (WindowThumbnailHost) barındıran kontrol.
/// WindowHandle değiştiğinde eski kancayı çözer ve yenisini güvenle bağlar.
/// </summary>
public sealed class WindowThumbnailPresenter : ContentControl
{
    public static readonly DependencyProperty WindowHandleProperty =
        DependencyProperty.Register(
            nameof(WindowHandle),
            typeof(nint),
            typeof(WindowThumbnailPresenter),
            new PropertyMetadata(nint.Zero, OnWindowHandleChanged));

    public nint WindowHandle
    {
        get => (nint)GetValue(WindowHandleProperty);
        set => SetValue(WindowHandleProperty, value);
    }

    private static void OnWindowHandleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WindowThumbnailPresenter presenter)
            return;

        var newHandle = (nint)e.NewValue;
        var oldHandle = (nint)e.OldValue;
        if (newHandle == oldHandle && presenter.Content is not null)
            return;

        if (presenter.Content is WindowThumbnailHost oldHost)
        {
            oldHost.Dispose();
            presenter.Content = null;
        }

        if (newHandle != 0)
        {
            presenter.Content = new WindowThumbnailHost(newHandle);
        }
    }
}

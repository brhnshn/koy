using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Koy.App.Features.Settings.Pages;

public partial class KisayollarPage : UserControl
{
    public KisayollarPage()
    {
        InitializeComponent();
    }

    private static ShortcutRowViewModel? RowOf(object sender) =>
        (sender as FrameworkElement)?.DataContext as ShortcutRowViewModel;

    /// <summary>Kutuya tıklanınca yakalama başlar ve klavye odağı kutuya geçer.</summary>
    private void OnCaptureClick(object sender, RoutedEventArgs e)
    {
        if (RowOf(sender) is not { } row)
            return;

        row.StartCaptureCommand.Execute(null);
        Keyboard.Focus((IInputElement)sender);
    }

    private void OnCapturePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (RowOf(sender) is not { IsCapturing: true } row)
            return;

        // Alt ile birlikte basılan tuşlar Key.System olarak gelir; gerçek tuş SystemKey'de durur.
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.ImeProcessed)
            key = e.ImeProcessedKey;

        if (row.HandleKey(key, Keyboard.Modifiers))
            e.Handled = true;
    }

    /// <summary>Odak başka yere giderse yakalama iptal edilir (kutu "bekliyor" durumunda takılı kalmasın).</summary>
    private void OnCaptureLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        RowOf(sender)?.CancelCapture();
    }
}

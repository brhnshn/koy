using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Koy.App.Features.Dock;

public partial class DockCapsule : UserControl
{
    private DockViewModel? ViewModel => DataContext as DockViewModel;

    public DockCapsule()
    {
        InitializeComponent();
    }

    private void OnItemLeftClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DockItemViewModel item })
        {
            ViewModel?.ClosePreviewNow();
            item.ClickCommand.Execute(null);
        }
    }

    private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle &&
            sender is FrameworkElement { DataContext: DockItemViewModel item })
        {
            ViewModel?.ClosePreviewNow();
            item.MiddleClickCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnItemContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        e.Handled = true; // Menü açılışını OnItemRightClick ve DockContextMenuManager yönetir
    }

    private void OnItemRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.ContextMenu is not null)
        {
            ViewModel?.ClosePreviewNow();
            DockContextMenuManager.Open(element.ContextMenu, element);
            e.Handled = true;
        }
    }

    private void OnItemMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement element &&
            element.DataContext is DockItemViewModel item &&
            ViewModel is not null)
        {
            var index = ViewModel.Items.IndexOf(item);
            if (index >= 0)
            {
                ViewModel.OnItemHover(index);
            }

            ViewModel.OnItemMouseEnter(item, element);
        }
    }

    private void OnItemMouseLeave(object sender, MouseEventArgs e)
    {
        ViewModel?.OnItemMouseLeave();
        ViewModel?.OnHoverEnded();
    }
}

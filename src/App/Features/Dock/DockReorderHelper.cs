using System.Windows;
using System.Windows.Input;
using Koy.Core.Dock;

namespace Koy.App.Features.Dock;

/// <summary>
/// Dock simgesini fareyle sürükleyip sıralamayı yöneten fare mantığı (yüzen kapsül ve tam genişlik görünümü paylaşır).
/// Sürükleme eşiği aşılmadan tıklama normal çalışır; eşik aşılınca fare yakalanır, bırakınca tıklama yutulur.
/// Hesaplar Core'daki <see cref="DockReorderMath"/>, kayma yayları <see cref="DockViewModel"/> içindedir.
/// </summary>
internal sealed class DockReorderHelper(Func<DockViewModel?> viewModel)
{
    private FrameworkElement? _element;
    private DockItemViewModel? _item;
    private Point _start;
    private bool _dragging;
    private int _originalIndex;
    private double _slotWidth;
    private double _lastOffset;

    public void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left ||
            sender is not FrameworkElement { DataContext: DockItemViewModel item } element ||
            viewModel() is not { } vm || !vm.CanStartReorder(item))
            return;

        _element = element;
        _item = item;
        _dragging = false;
        _start = Position(e);
    }

    public void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_item is null || _element is null || viewModel() is not { } vm)
            return;

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            Reset(vm, cancel: _dragging);
            return;
        }

        var position = Position(e);
        if (!_dragging)
        {
            if (!DockReorderMath.ExceedsThreshold(
                    position.X - _start.X, position.Y - _start.Y, SystemParameters.MinimumHorizontalDragDistance))
                return;

            _originalIndex = vm.Items.IndexOf(_item);
            if (_originalIndex < 0)
            {
                Reset(vm, cancel: false);
                return;
            }

            _slotWidth = _element.ActualWidth + _element.Margin.Left + _element.Margin.Right;
            _dragging = true;
            _element.CaptureMouse();
            vm.BeginReorder();
        }

        // İmleç yalnız sabitli yuvalar arasında gezer: sınırın ötesine kayma kırpılır.
        var count = vm.PinnedCount;
        var min = -_originalIndex * _slotWidth;
        var max = (count - 1 - _originalIndex) * _slotWidth;
        _lastOffset = Math.Clamp(position.X - _start.X, min, max);
        vm.UpdateReorder(_item, _originalIndex, _lastOffset, _slotWidth);
    }

    /// <summary>Bırakma: sürükleme sürmüşse tamamlar ve true döner (çağıran tıklamayı işlemez).</summary>
    public bool TryFinish()
    {
        if (_item is null)
            return false;

        var wasDragging = _dragging;
        var vm = viewModel();
        if (wasDragging && vm is not null)
        {
            var item = _item;
            var element = _element;
            Clear();
            element?.ReleaseMouseCapture();
            vm.CommitReorder(item, _originalIndex, _lastOffset, _slotWidth);
            return true;
        }

        Clear();
        return false;
    }

    /// <summary>Fare yakalaması başka yere geçtiyse sürükleme iptal edilir.</summary>
    public void OnLostCapture()
    {
        if (_dragging && viewModel() is { } vm)
            Reset(vm, cancel: true);
    }

    private void Reset(DockViewModel vm, bool cancel)
    {
        var element = _element;
        Clear();
        element?.ReleaseMouseCapture();
        if (cancel)
            vm.CancelReorder();
    }

    private void Clear()
    {
        _element = null;
        _item = null;
        _dragging = false;
    }

    private Point Position(MouseEventArgs e)
        => e.GetPosition(_element is null ? null : Window.GetWindow(_element));
}

using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Features.Settings;
using Koy.Core.Animation;
using Koy.Core.Dock;
using Koy.Core.Settings;
using Koy.Core.Shell;
using Koy.Core.Visibility;

namespace Koy.App.Features.Dock;

/// <summary>
/// Dock penceresi için ana ViewModel. Sabitli ve açık uygulamaları, başlat menüsü ve tepsi eylemlerini bağlar.
/// </summary>
public sealed partial class DockViewModel : ObservableObject, IDisposable
{
    private readonly AppSettings _settings;
    private readonly DockController _controller;
    private readonly IShellActionService _shellActionService;
    private readonly VisibilityController? _visibilityController;

    private readonly Spring _translateSpring;
    private readonly Spring _opacitySpring;
    private bool _isAnimating;
    private bool _disposed;

    [ObservableProperty]
    private double _translationY;

    [ObservableProperty]
    private double _opacity = 1.0;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _magnificationEnabled;

    [ObservableProperty]
    private string _accentColor;

    public ObservableCollection<DockItemViewModel> Items { get; } = [];

    public event EventHandler? AnimationNeeded;

    public DockViewModel(
        AppSettings settings,
        DockController controller,
        IShellActionService shellActionService,
        VisibilityController? visibilityController = null)
    {
        _settings = settings;
        _controller = controller;
        _shellActionService = shellActionService;
        _visibilityController = visibilityController;

        MagnificationEnabled = settings.DockHoverMagnify;
        _accentColor = settings.AccentColor ?? "#0078D4";

        _translateSpring = new Spring(0) { Stiffness = 280, Damping = 25 };
        _opacitySpring = new Spring(1.0) { Stiffness = 200, Damping = 24 };

        _controller.ItemsChanged += OnControllerItemsChanged;
        _controller.FullscreenStateChanged += OnFullscreenStateChanged;

        if (_visibilityController is not null)
        {
            _visibilityController.Changed += OnVisibilityChanged;
        }

        UpdateItems();
    }

    [RelayCommand]
    private void StartMenu()
    {
        _shellActionService.SendWinKey();
    }

    [RelayCommand]
    private void SystemTray()
    {
        _shellActionService.ToggleSystemTray();
    }

    [RelayCommand]
    private static void OpenSettings()
    {
        var store = new SettingsStore(SettingsStore.DefaultPath);
        var win = new SettingsWindow
        {
            DataContext = new SettingsViewModel(store)
        };
        win.ShowDialog();
    }

    [RelayCommand]
    private static void Exit()
    {
        Application.Current.Shutdown();
    }

    public void OnItemHover(int hoveredIndex)
    {
        if (!MagnificationEnabled)
            return;

        for (var i = 0; i < Items.Count; i++)
        {
            var dist = Math.Abs(i - hoveredIndex);
            if (dist == 0)
                Items[i].Scale = 1.28;
            else if (dist == 1)
                Items[i].Scale = 1.14;
            else
                Items[i].Scale = 1.0;
        }
    }

    public void OnHoverEnded()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            Items[i].Scale = 1.0;
        }
    }

    public void UpdateSprings(double deltaSeconds)
    {
        if (!_isAnimating)
            return;

        var tMoving = _translateSpring.Step(deltaSeconds);
        var oMoving = _opacitySpring.Step(deltaSeconds);

        TranslationY = _translateSpring.Value;
        Opacity = Math.Clamp(_opacitySpring.Value, 0.0, 1.0);

        if (!tMoving && !oMoving)
        {
            _isAnimating = false;
        }
    }

    private void UpdateItems()
    {
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            var models = _controller.Items;

            // Mevcut öğeleri eşleştirerek listeyi güncelle (sıfırdan yaratmadan görsel titremeyi önle)
            var newVms = new List<DockItemViewModel>();
            foreach (var model in models)
            {
                var existing = Items.FirstOrDefault(vm => vm.Model.Id == model.Id);
                if (existing is not null)
                {
                    existing.Update(model);
                    newVms.Add(existing);
                }
                else
                {
                    newVms.Add(new DockItemViewModel(model, _controller));
                }
            }

            Items.Clear();
            foreach (var vm in newVms)
            {
                Items.Add(vm);
            }
        });
    }

    private void OnControllerItemsChanged(object? sender, EventArgs e)
    {
        UpdateItems();
    }

    private void OnFullscreenStateChanged(object? sender, bool isFullscreen)
    {
        ApplyVisibilityState();
    }

    private void OnVisibilityChanged(object? sender, VisibilityResult result)
    {
        ApplyVisibilityState();
    }

    public void SetMouseAtBottom(bool atBottom)
    {
        if (_visibilityController is not null && _settings.DockVisibility != VisibilityMode.Fixed)
        {
            _visibilityController.SetMouseAtEdge(atBottom);
        }
    }

    private void ApplyVisibilityState()
    {
        var shouldShow = !_controller.IsHiddenByFullscreen;

        if (_visibilityController is not null)
        {
            shouldShow = shouldShow && _visibilityController.IsVisible;
        }

        IsVisible = shouldShow;

        var targetY = shouldShow ? 0.0 : 80.0;
        var targetOpacity = shouldShow ? 1.0 : 0.0;

        _translateSpring.Target = targetY;
        _opacitySpring.Target = targetOpacity;

        _isAnimating = true;
        AnimationNeeded?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _controller.ItemsChanged -= OnControllerItemsChanged;
        _controller.FullscreenStateChanged -= OnFullscreenStateChanged;

        if (_visibilityController is not null)
        {
            _visibilityController.Changed -= OnVisibilityChanged;
        }
    }
}

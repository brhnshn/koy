using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Features.Dock.Tray;
using Koy.App.Features.Settings;
using Koy.Core.Animation;
using Koy.Core.Display;
using Koy.Core.Dock;
using Koy.Core.Media;
using Koy.Core.Settings;
using Koy.Core.Shell;
using Koy.Core.Tray;
using Koy.Core.Visibility;
using Koy.Platform.Shell;

namespace Koy.App.Features.Dock;

/// <summary>
/// Dock penceresi için ana ViewModel.
/// Yüzen kapsül ve tam genişlik modlarını, yakınlık davranışlarını (DockDown, ReserveSpace, Dodge, AlwaysHidden)
/// ve 400 ms DWM pencere küçük resim önizlemesini yönetir.
/// </summary>
public sealed partial class DockViewModel : ObservableObject, IDisposable
{
    private readonly AppSettings _settings;
    private readonly DockController _controller;
    private readonly IShellActionService _shellActionService;
    private readonly VisibilityController? _visibilityController;
    private readonly IMediaService? _mediaService;
    private ITrayService? _trayService;
    // Tepsi iş parçacığı okur, arayüz iş parçacığı yazar: kapalıyken simge olayları arayüzü uyandırmasın.
    private volatile bool _trayPopupOpen;
    private int _traySyncQueued;
    private DispatcherTimer? _trayCloseTimer;

    private readonly Spring _translateSpring;
    private readonly Spring _opacitySpring;
    private bool _isAnimating;
    private bool _disposed;

    /// <summary>Pil tasarrufu: true iken dock kayma ve solma animasyonu olmadan anında konum/görünürlük değiştirir.</summary>
    public bool ReduceMotion { get; set; }

    private DisplayMonitor? _currentMonitor;
    private WindowRect _foregroundWindowBounds = WindowRect.Empty;
    private bool _isForegroundWindowMaximized;
    private bool _isForegroundWindowFullscreen;
    private bool _isMouseAtBottomEdge;
    private bool _isMouseOverDock;
    private bool _isMouseOverPopup;
    private double _lastContentWidth = 520.0;

    private readonly DispatcherTimer _thumbnailHoverTimer;
    private readonly DispatcherTimer _previewCloseTimer;
    private readonly DispatcherTimer _dodgeHoverTimer;
    private DockItemViewModel? _pendingPreviewItem;

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

    // Düzen ve yakınlık modu durumu
    [ObservableProperty]
    private bool _isFullWidthMode;

    [ObservableProperty]
    private bool _isSnapped;

    [ObservableProperty]
    private bool _isHandleVisible;

    [ObservableProperty]
    private double _dockHeight = 60.0;

    [ObservableProperty]
    private Thickness _dockMargin = new(12, 12, 12, 10);

    [ObservableProperty]
    private CornerRadius _capsuleCornerRadius = new(18);

    [ObservableProperty]
    private bool _showStartButton = true;

    [ObservableProperty]
    private bool _showFolders = true;

    [ObservableProperty]
    private bool _showMinimized = true;

    [ObservableProperty]
    private bool _showBadges = true;

    // Sistem tepsisi: simgeler yalnız pencere açıkken eşitlenir ve çözülür (boştayken iş yok).
    [ObservableProperty]
    private bool _isTrayPopupOpen;

    [ObservableProperty]
    private bool _hasTrayIcons;

    /// <summary>Tepsi düğmesi yalnız tepsi penceresi gerçekten çalışıyorsa görünür (başlatma başarısızsa gizli).</summary>
    public bool HasTray => _trayService is { IsRunning: true };

    /// <summary>Ayırıcının sağında bir şey varsa (klasörler ya da tepsi) ayırıcı görünür.</summary>
    public bool ShowRightSection => ShowFolders || HasTray;

    public ObservableCollection<TrayIconViewModel> TrayIcons { get; } = [];

    // Tam Genişlik Modu: Çalan müzik hapı (Sol bölüm)
    [ObservableProperty]
    private string _nowPlayingTitle = "Şarkı çalınmıyor";

    [ObservableProperty]
    private string _nowPlayingArtist = "Koy Müzik";

    [ObservableProperty]
    private ImageSource? _nowPlayingCover;

    [ObservableProperty]
    private bool _hasNowPlaying;

    [ObservableProperty]
    private double _iconSize = 28.0;

    [ObservableProperty]
    private double _itemSize = 38.0;

    // DWM Canlı Önizleme Açılır Penceresi
    [ObservableProperty]
    private bool _isPreviewPopupOpen;

    [ObservableProperty]
    private DockItemViewModel? _previewItem;

    [ObservableProperty]
    private UIElement? _previewPlacementTarget;

    public ObservableCollection<DockItemViewModel> Items { get; } = [];

    public event EventHandler? AnimationNeeded;
    public event EventHandler? AnimationStopped;
    public event Action? RequestReposition;

    public DockController Controller => _controller;
    public AppSettings Settings => _settings;
    public DockStatusViewModel? Status { get; }

    public DockViewModel(
        AppSettings settings,
        DockController controller,
        IShellActionService shellActionService,
        VisibilityController? visibilityController = null,
        DockStatusViewModel? status = null,
        IMediaService? mediaService = null,
        ITrayService? trayService = null)
    {
        _settings = settings;
        Status = status;
        _controller = controller;
        _shellActionService = shellActionService;
        _visibilityController = visibilityController;
        _mediaService = mediaService;
        _trayService = trayService;

        _iconSize = settings.DockIconSize > 0 ? settings.DockIconSize : 28.0;
        _itemSize = _iconSize + 10.0;

        _magnificationEnabled = settings.DockHoverMagnify;
        _accentColor = settings.AccentColor ?? "#0078D4";

        _isFullWidthMode = settings.DockStyle == DockStyleMode.FullWidth;
        _showStartButton = settings.DockShowStartButton;
        _showFolders = settings.DockShowFolders;
        _showMinimized = settings.DockShowMinimized;
        _showBadges = settings.DockShowBadges;

        _translateSpring = new Spring(0) { Stiffness = 280, Damping = 25 };
        _opacitySpring = new Spring(1.0) { Stiffness = 200, Damping = 24 };

        _thumbnailHoverTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(50, settings.DockThumbnailDelayMs))
        };
        _thumbnailHoverTimer.Tick += OnThumbnailTimerTick;

        _previewCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _previewCloseTimer.Tick += OnPreviewCloseTimerTick;

        _dodgeHoverTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(50, settings.DockDodgeDelayMs))
        };
        _dodgeHoverTimer.Tick += OnDodgeHoverTimerTick;

        _controller.ItemsChanged += OnControllerItemsChanged;
        _controller.FullscreenStateChanged += OnFullscreenStateChanged;

        if (_visibilityController is not null)
        {
            _visibilityController.Changed += OnVisibilityChanged;
        }

        if (_mediaService is not null)
        {
            _mediaService.Changed += OnMediaChanged;
            UpdateMediaInfo(_mediaService.Current);
        }

        DockContextMenuManager.ActiveMenuChanged += OnActiveMenuChanged;

        if (_trayService is not null)
        {
            _trayService.IconsChanged += OnTrayIconsChanged;
        }

        UpdateItems();
    }

    /// <summary>
    /// Tepsi servisini takar ya da çıkarır (görev çubuğu gizlenince / geri gelince). Arayüz iş parçacığında çağrılır.
    /// Çıkarılınca açık pencere kapanır ve simge görüntüleri bırakılır.
    /// </summary>
    public void SetTrayService(ITrayService? service)
    {
        if (ReferenceEquals(_trayService, service))
            return;

        if (_trayService is not null)
            _trayService.IconsChanged -= OnTrayIconsChanged;

        _trayService = service;

        if (service is not null)
            service.IconsChanged += OnTrayIconsChanged;

        IsTrayPopupOpen = false;
        TrayIcons.Clear();
        HasTrayIcons = false;
        OnPropertyChanged(nameof(HasTray));
        OnPropertyChanged(nameof(ShowRightSection));
    }

    [RelayCommand]
    private void ToggleTray()
    {
        if (_trayService is null)
            return;

        if (IsTrayPopupOpen)
        {
            IsTrayPopupOpen = false;
            return;
        }

        ClosePreviewNow();
        _trayService.RemoveDeadIcons();
        SyncTrayIcons();
        IsTrayPopupOpen = true;
    }

    /// <summary>Tepsi simgesine tıklandı: uygulamaya Windows görev çubuğunun göndereceği mesajları iletir.</summary>
    public void SendTrayMouse(TrayIconViewModel icon, TrayMouseAction action)
    {
        if (_trayService is null)
            return;

        CursorHelper.GetPosition(out var x, out var y);
        _trayService.SendMouse(icon.Key, action, x, y);

        if (action == TrayMouseAction.LeftClick)
        {
            // Bazı uygulamalar çift tıklama bekler; pencere çift tıklama süresi kadar açık kalır.
            _trayCloseTimer ??= CreateTrayCloseTimer();
            _trayCloseTimer.Stop();
            _trayCloseTimer.Start();
        }
        else
        {
            // Uygulamanın menüsü ya da penceresi açılıyor; tepsi penceresi önünü kapatmasın.
            IsTrayPopupOpen = false;
        }
    }

    private DispatcherTimer CreateTrayCloseTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(CursorHelper.DoubleClickTimeMs) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            IsTrayPopupOpen = false;
        };
        return timer;
    }

    partial void OnIsTrayPopupOpenChanged(bool value)
    {
        _trayPopupOpen = value;
        if (!value)
        {
            _trayCloseTimer?.Stop();
            // Kapalıyken görüntüler bellekte tutulmaz.
            TrayIcons.Clear();
        }

        EvaluatePosition(_lastContentWidth);
    }

    private void OnTrayIconsChanged(object? sender, EventArgs e)
    {
        // Tepsi iş parçacığından gelir. Pencere kapalıyken arayüze hiç dönülmez: açılırken liste zaten baştan okunur.
        if (!_trayPopupOpen)
            return;

        // Sık değişen simgelerde (animasyonlu ipucu, rozet) arka arkaya iş biriktirme: bekleyen tek bir eşitleme yeter.
        if (Interlocked.Exchange(ref _traySyncQueued, 1) == 1)
            return;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            Volatile.Write(ref _traySyncQueued, 0);
            return;
        }

        dispatcher.InvokeAsync(() =>
        {
            Volatile.Write(ref _traySyncQueued, 0);
            if (IsTrayPopupOpen)
                SyncTrayIcons();
        }, DispatcherPriority.Background);
    }

    private void SyncTrayIcons()
    {
        if (_trayService is null)
            return;

        var models = _trayService.GetVisibleIcons();

        for (var i = TrayIcons.Count - 1; i >= 0; i--)
        {
            var key = TrayIcons[i].Key;
            if (!models.Any(m => m.Key == key))
                TrayIcons.RemoveAt(i);
        }

        for (var targetIndex = 0; targetIndex < models.Count; targetIndex++)
        {
            var model = models[targetIndex];
            var existingIndex = -1;
            for (var j = 0; j < TrayIcons.Count; j++)
            {
                if (TrayIcons[j].Key == model.Key)
                {
                    existingIndex = j;
                    break;
                }
            }

            TrayIconViewModel vm;
            if (existingIndex >= 0)
            {
                vm = TrayIcons[existingIndex];
                if (existingIndex != targetIndex)
                    TrayIcons.Move(existingIndex, targetIndex);
            }
            else
            {
                vm = new TrayIconViewModel(model);
                TrayIcons.Insert(targetIndex, vm);
            }

            vm.Update(model, _trayService.GetIconImage);
        }

        HasTrayIcons = TrayIcons.Count > 0;
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

    public static Action? OpenKoyMenuHandler { get; set; }

    [RelayCommand]
    private void OpenKoyMenu()
    {
        // Sağ alt Koy menüsü veya ayarlar
        if (OpenKoyMenuHandler is not null)
        {
            OpenKoyMenuHandler();
            return;
        }
        OpenSettings();
    }

    [RelayCommand]
    private static void OpenDownloads()
    {
        try
        {
            var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloadsPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = downloadsPath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // İhmal et
        }
    }

    [RelayCommand]
    private void OpenNotchMusic()
    {
        // Çentik müzik paneli kısayolu
        _visibilityController?.OnMediaActivity();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        KoyWindow.ShowInstance(2);
    }

    public void SetMonitor(DisplayMonitor? monitor)
    {
        _currentMonitor = monitor;
        EvaluatePosition(_lastContentWidth);
    }

    public void UpdateForegroundWindow(WindowRect bounds, bool isMaximized, bool isFullscreen = false)
    {
        _foregroundWindowBounds = bounds;
        _isForegroundWindowMaximized = isMaximized;
        _isForegroundWindowFullscreen = isFullscreen;
        EvaluatePosition(_lastContentWidth);
    }

    public void SetMouseAtBottom(bool atBottom)
    {
        if (_isMouseAtBottomEdge == atBottom)
            return;

        _isMouseAtBottomEdge = atBottom;

        if (atBottom && _settings.DockProximity == DockProximityMode.Dodge)
        {
            _dodgeHoverTimer.Start();
        }
        else
        {
            _dodgeHoverTimer.Stop();
            EvaluatePosition(_lastContentWidth);
        }
    }

    public void SetMouseOverDock(bool mouseOver)
    {
        _isMouseOverDock = mouseOver;
        EvaluatePosition(_lastContentWidth);
    }

    private void OnDodgeHoverTimerTick(object? sender, EventArgs e)
    {
        _dodgeHoverTimer.Stop();
        EvaluatePosition(_lastContentWidth);
    }

    public void EvaluatePosition(double contentWidth)
    {
        if (contentWidth > 0)
            _lastContentWidth = contentWidth;

        var effectiveMouseOver = _isMouseOverDock || _isMouseOverPopup || IsPreviewPopupOpen || IsTrayPopupOpen || DockContextMenuManager.HasActiveMenu;

        var resolved = DockPositionResolver.Resolve(
            _currentMonitor,
            _settings.DockStyle,
            _settings.DockProximity,
            _lastContentWidth,
            _foregroundWindowBounds,
            _isForegroundWindowMaximized,
            _isMouseAtBottomEdge,
            effectiveMouseOver,
            _isForegroundWindowFullscreen,
            _settings.DockHideInFullscreen);

        IsFullWidthMode = resolved.IsFullWidth;
        IsSnapped = resolved.IsSnapped;
        IsHandleVisible = resolved.IsHandleVisible;
        DockHeight = resolved.HeightDip;

        if (resolved.IsFullWidth)
        {
            DockMargin = new Thickness(0);
            CapsuleCornerRadius = new CornerRadius(0);
        }
        else if (resolved.IsSnapped)
        {
            DockMargin = new Thickness(12, 12, 12, 0);
            CapsuleCornerRadius = new CornerRadius(
                resolved.CornerRadiusTopLeft,
                resolved.CornerRadiusTopRight,
                resolved.CornerRadiusBottomRight,
                resolved.CornerRadiusBottomLeft);
        }
        else
        {
            DockMargin = new Thickness(12, 12, 12, 10);
            CapsuleCornerRadius = new CornerRadius(18);
        }

        var isBlockedByFullscreen = _settings.DockHideInFullscreen && (_controller.IsHiddenByFullscreen || _isForegroundWindowFullscreen);
        var shouldShow = !resolved.IsHidden && !isBlockedByFullscreen;
        if (_visibilityController is not null && _settings.DockProximity != DockProximityMode.ReserveSpace)
        {
            shouldShow = shouldShow && _visibilityController.IsVisible;
        }

        IsVisible = shouldShow;
        Status?.SetActive(shouldShow);

        var targetY = shouldShow ? resolved.TranslationY : (resolved.IsFullWidth ? (DockPositionResolver.FullWidthHeightDip + 10.0) : 80.0);
        var targetOpacity = shouldShow ? resolved.Opacity : 0.0;

        if (ReduceMotion)
        {
            _translateSpring.SnapTo(targetY);
            _opacitySpring.SnapTo(targetOpacity);
        }
        else
        {
            _translateSpring.Target = targetY;
            _opacitySpring.Target = targetOpacity;
        }

        _isAnimating = true;
        AnimationNeeded?.Invoke(this, EventArgs.Empty);

        RequestReposition?.Invoke();
    }

    // DWM Canlı Önizleme Tetikleyicisi
    public void OnItemMouseEnter(DockItemViewModel item, UIElement element)
    {
        _previewCloseTimer.Stop();

        if (!_settings.DockThumbnailPreview || !item.HasOpenWindows)
        {
            _thumbnailHoverTimer.Stop();
            IsPreviewPopupOpen = false;
            PreviewItem = null;
            _pendingPreviewItem = null;
            return;
        }

        _pendingPreviewItem = item;
        PreviewPlacementTarget = element;

        // Önizleme kutucuğu halihazırda açıksa başka simgeye geçişte hemen güncelle ve yeniden konumlandır
        if (IsPreviewPopupOpen)
        {
            _thumbnailHoverTimer.Stop();
            PreviewItem = item;
            // WPF Popup PlacementTarget değiştiğinde HWND konumunu güncellemesi için IsOpen tazelenir
            IsPreviewPopupOpen = false;
            IsPreviewPopupOpen = true;
            return;
        }

        _thumbnailHoverTimer.Stop();
        _thumbnailHoverTimer.Start();
    }

    public void OnItemMouseLeave()
    {
        _thumbnailHoverTimer.Stop();
        _pendingPreviewItem = null;

        if (IsPreviewPopupOpen)
        {
            _previewCloseTimer.Stop();
            _previewCloseTimer.Start();
        }
        else
        {
            PreviewItem = null;
        }
    }

    public void OnPopupMouseEnter()
    {
        _isMouseOverPopup = true;
        _previewCloseTimer.Stop();
        EvaluatePosition(_lastContentWidth);
    }

    public void OnPopupMouseLeave()
    {
        _isMouseOverPopup = false;
        _previewCloseTimer.Stop();
        _previewCloseTimer.Start();
    }

    private void OnPreviewCloseTimerTick(object? sender, EventArgs e)
    {
        _previewCloseTimer.Stop();
        _isMouseOverPopup = false;
        IsPreviewPopupOpen = false;
        PreviewItem = null;
        _pendingPreviewItem = null;
        EvaluatePosition(_lastContentWidth);
    }

    public void ClosePreviewNow()
    {
        _thumbnailHoverTimer.Stop();
        _previewCloseTimer.Stop();
        _pendingPreviewItem = null;
        _isMouseOverPopup = false;
        IsPreviewPopupOpen = false;
        PreviewItem = null;
        EvaluatePosition(_lastContentWidth);
    }

    private void OnThumbnailTimerTick(object? sender, EventArgs e)
    {
        _thumbnailHoverTimer.Stop();
        if (_pendingPreviewItem is not null && _pendingPreviewItem.HasOpenWindows)
        {
            PreviewItem = _pendingPreviewItem;
            IsPreviewPopupOpen = true;
        }
    }

    public void OnItemHover(int hoveredIndex)
    {
        if (!MagnificationEnabled)
            return;

        for (var i = 0; i < Items.Count; i++)
        {
            var dist = Math.Abs(i - hoveredIndex);
            var targetScale = dist switch
            {
                0 => 1.30,
                1 => 1.15,
                _ => 1.0
            };
            Items[i].SetTargetScale(targetScale);
        }

        _isAnimating = true;
        AnimationNeeded?.Invoke(this, EventArgs.Empty);
    }

    public void OnHoverEnded()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            Items[i].SetTargetScale(1.0);
        }

        _isAnimating = true;
        AnimationNeeded?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSprings(double deltaSeconds)
    {
        if (!_isAnimating)
            return;

        var tMoving = _translateSpring.Step(deltaSeconds);
        var oMoving = _opacitySpring.Step(deltaSeconds);

        TranslationY = _translateSpring.Value;
        Opacity = Math.Clamp(_opacitySpring.Value, 0.0, 1.0);

        var scaleMoving = false;
        foreach (var item in Items)
        {
            if (item.StepScale(deltaSeconds))
                scaleMoving = true;
        }

        if (!tMoving && !oMoving && !scaleMoving)
        {
            _isAnimating = false;
            AnimationStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateItems()
    {
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            var models = _controller.Items;

            // 1. Artık açık veya sabitli olmayan öğeleri kaldır
            var modelIds = new HashSet<string>(models.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);
            for (var i = Items.Count - 1; i >= 0; i--)
            {
                if (!modelIds.Contains(Items[i].Model.Id))
                {
                    Items.RemoveAt(i);
                }
            }

            // 2. Sıralamayı ve öğeleri yerinde güncelle (Items.Clear yapılmaz, görsel zıplama ve titreme olmaz)
            for (var targetIndex = 0; targetIndex < models.Count; targetIndex++)
            {
                var model = models[targetIndex];
                var existingIndex = -1;
                for (var j = 0; j < Items.Count; j++)
                {
                    if (string.Equals(Items[j].Model.Id, model.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        existingIndex = j;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    Items[existingIndex].IconSize = IconSize;
                    Items[existingIndex].ItemSize = ItemSize;
                    Items[existingIndex].Update(model);
                    if (existingIndex != targetIndex && existingIndex < Items.Count && targetIndex < Items.Count)
                    {
                        Items.Move(existingIndex, targetIndex);
                    }
                }
                else
                {
                    var newVm = new DockItemViewModel(model, _controller)
                    {
                        IconSize = IconSize,
                        ItemSize = ItemSize,
                        UseLineIndicator = _settings.DockIndicatorStyle == 1
                    };
                    if (targetIndex < Items.Count)
                        Items.Insert(targetIndex, newVm);
                    else
                        Items.Add(newVm);
                }
            }

            EvaluatePosition(_lastContentWidth);
        });
    }

    private void OnMediaChanged(object? sender, MediaInfo? info)
    {
        Application.Current?.Dispatcher.InvokeAsync(() => UpdateMediaInfo(info));
    }

    private void UpdateMediaInfo(MediaInfo? info)
    {
        if (info is null || !info.IsPlaying)
        {
            HasNowPlaying = false;
            NowPlayingTitle = "Çalan müzik yok";
            NowPlayingArtist = "Koy";
            NowPlayingCover = null;
            return;
        }

        HasNowPlaying = true;
        NowPlayingTitle = !string.IsNullOrWhiteSpace(info.Title) ? info.Title : "Bilinmeyen Parça";
        NowPlayingArtist = !string.IsNullOrWhiteSpace(info.Artist) ? info.Artist : "Bilinmeyen Sanatçı";

        if (info.Thumbnail is { Length: > 0 })
        {
            try
            {
                using var ms = new MemoryStream(info.Thumbnail);
                var img = new BitmapImage();
                img.BeginInit();
                img.DecodePixelWidth = 28;
                img.StreamSource = ms;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();
                NowPlayingCover = img;
            }
            catch
            {
                NowPlayingCover = null;
            }
        }
        else
        {
            NowPlayingCover = null;
        }
    }

    private void OnControllerItemsChanged(object? sender, EventArgs e) => UpdateItems();
    private void OnFullscreenStateChanged(object? sender, bool isFullscreen) => EvaluatePosition(_lastContentWidth);
    private void OnVisibilityChanged(object? sender, VisibilityResult result)
    {
        // Medya peek zamanlayıcısı ThreadPool'dan tikler; EvaluatePosition pencereye dokunduğu için UI'a taşı.
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            EvaluatePosition(_lastContentWidth);
            return;
        }

        dispatcher.InvokeAsync(() =>
        {
            if (!_disposed)
                EvaluatePosition(_lastContentWidth);
        });
    }
    private void OnActiveMenuChanged(object? sender, EventArgs e) => EvaluatePosition(_lastContentWidth);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _thumbnailHoverTimer.Stop();
        _previewCloseTimer.Stop();
        _dodgeHoverTimer.Stop();

        DockContextMenuManager.ActiveMenuChanged -= OnActiveMenuChanged;

        _controller.ItemsChanged -= OnControllerItemsChanged;
        _controller.FullscreenStateChanged -= OnFullscreenStateChanged;

        if (_visibilityController is not null)
        {
            _visibilityController.Changed -= OnVisibilityChanged;
        }

        if (_mediaService is not null)
        {
            _mediaService.Changed -= OnMediaChanged;
        }

        if (_trayService is not null)
        {
            _trayService.IconsChanged -= OnTrayIconsChanged;
            _trayService = null;
        }

        _trayCloseTimer?.Stop();
    }
}

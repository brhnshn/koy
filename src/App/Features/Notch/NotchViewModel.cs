using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Features.Calendar;
using Koy.App.Features.ControlCenter;
using Koy.App.Features.Music;
using Koy.App.Features.Notch.OSD;
using Koy.App.Resources;
using Koy.App.Features.Settings;
using Koy.App.Features.Shelf;
using Koy.App.Features.Status;
using Koy.Core.Notch;
using Koy.Core.Settings;
using Koy.Core.Visibility;

namespace Koy.App.Features.Notch;

public sealed partial class NotchViewModel : ObservableObject, IDisposable
{
    private readonly NotchController _controller;
    private readonly MusicViewModel _music;
    private readonly ControlCenterViewModel _controlCenter;
    private readonly StatusViewModel _status;
    private readonly CalendarViewModel _calendar;
    private readonly ShelfViewModel _shelf;
    private readonly VisibilityController _visibility;
    private readonly Dispatcher _dispatcher;
    private DispatcherTimer? _clockTimer;
    private bool _disposed;

    public AppSettings Settings { get; }
    public NotchController Controller => _controller;
    public MusicViewModel Music => _music;
    public ControlCenterViewModel ControlCenter => _controlCenter;
    public StatusViewModel Status => _status;
    public CalendarViewModel Calendar => _calendar;
    public ShelfViewModel Shelf => _shelf;
    public VisibilityController Visibility => _visibility;
    public OsdIndicatorViewModel Osd { get; }

    /// <summary>Boşta saat gösterilsin mi (ayar: Saat ve pil / Yalnız saat / Hiçbiri).</summary>
    public bool ShowIdleClock => IdleDisplay.ShowClock(Settings.IdleDisplayMode);

    /// <summary>Boşta pil gösterilsin mi (ayar izin veriyorsa ve bilgisayarda pil varsa).</summary>
    public bool ShowIdleBattery => HasBattery && IdleDisplay.ShowBattery(Settings.IdleDisplayMode);

    /// <summary>Pil tasarrufu: pille çalışırken çentik animasyonsuz (anında) geçer.</summary>
    public bool ReduceMotion => Settings.BatterySaverReduceMotion && _status.IsOnBattery;

    /// <summary>Müzik çalarken dalga simgesi gösterilsin mi.</summary>
    public bool ShowMusicWaveform => Settings.MusicShowWaveform;

    /// <summary>Açık çentiğin üstündeki sekmeler; ayarlarda açık paneller, ayardaki sırayla.</summary>
    public ObservableCollection<NotchTabViewModel> Tabs { get; } = [];

    /// <summary>Kontrol paneli açıksa Wi-Fi / Bluetooth izlenir ve kapalı çentikte simgeleri gösterilebilir.</summary>
    public bool ShowRadioStatus { get; }

    /// <summary>Boşta Wi-Fi / Bluetooth simgeleri gösterilsin mi (Kontrol paneli açık ve boşta görünüm "saat, pil ve bağlantılar").</summary>
    public bool ShowIdleRadio => ShowRadioStatus && IdleDisplay.ShowRadio(Settings.IdleDisplayMode);

    public ObservableObject CurrentPanelContent => CurrentPanel switch
    {
        NotchPanel.Music => Music,
        NotchPanel.ControlCenter => ControlCenter,
        NotchPanel.Status => Status,
        NotchPanel.Calendar => Calendar,
        NotchPanel.Shelf => Shelf,
        _ => Music
    };

    [ObservableProperty]
    private NotchState _state = NotchState.Collapsed;

    [ObservableProperty]
    private NotchPanel _currentPanel = NotchPanel.Music;

    [ObservableProperty]
    private double _targetWidth = 236;

    [ObservableProperty]
    private double _targetHeight = 34;

    [ObservableProperty]
    private double _targetCornerRadius = 18;

    [ObservableProperty]
    private bool _isNotchVisible = true;

    [ObservableProperty]
    private string _title = "Koy";

    [ObservableProperty]
    private string _clockTime = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    private int _batteryPercent = 100;

    [ObservableProperty]
    private bool _isCharging;

    [ObservableProperty]
    private bool _hasBattery;

    partial void OnHasBatteryChanged(bool value) => OnPropertyChanged(nameof(ShowIdleBattery));

    [ObservableProperty]
    private double _batteryFillWidth = 17;

    public bool IsMusicPlaying => Music.HasTrack && Music.IsPlaying;

    public event Action<double, double>? TargetSizeChanged;
    public event Action<bool>? TargetVisibilityChanged;
    public event Action<int>? PanelTransitionTriggered;
    public event Action? ShakeRequested;

    public NotchViewModel(
        AppSettings settings,
        NotchController controller,
        MusicViewModel music,
        ControlCenterViewModel controlCenter,
        StatusViewModel status,
        CalendarViewModel calendar,
        ShelfViewModel shelf,
        VisibilityController? visibility = null,
        OsdIndicatorViewModel? osd = null,
        Dispatcher? dispatcher = null)
    {
        Settings = settings;
        _controller = controller;
        _music = music;
        _controlCenter = controlCenter;
        _status = status;
        _calendar = calendar;
        _shelf = shelf;
        _visibility = visibility ?? new VisibilityController(settings.NotchVisibility);
        _dispatcher = dispatcher ?? (System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher);
        Osd = osd ?? new OsdIndicatorViewModel(_dispatcher);

        _isNotchVisible = _visibility.IsVisible;

        _controller.StateChanged += OnStateChanged;
        _controller.PanelChanged += OnPanelChanged;
        _controller.PanelTransitionTriggered += OnPanelTransitionTriggered;
        _visibility.Changed += OnVisibilityChanged;
        _music.TrackChanged += OnTrackChanged;
        _music.PropertyChanged += OnMusicPropertyChanged;
        _calendar.TimerFinished += OnCalendarTimerFinished;
        _status.PropertyChanged += OnStatusPropertyChanged;
        Osd.CanShow = () => !_disposed && State != NotchState.Expanded;
        Osd.Activated += OnOsdActivated;
        Osd.Dismissed += OnOsdStateChanged;

        foreach (var panel in _controller.EnabledPanels)
            Tabs.Add(new NotchTabViewModel(panel, TabLabel(panel)) { IsSelected = panel == _controller.CurrentPanel });

        // Windows tepsisi gizli olduğu için Wi-Fi ve Bluetooth'un durumunu gösteren tek yer çentik.
        ShowRadioStatus = _controller.EnabledPanels.Contains(NotchPanel.ControlCenter);
        if (ShowRadioStatus)
            _controlCenter.StartRadioStatus();

        UpdateClock();
        UpdateBatteryState();
        UpdateTargetSize();

        if (_isNotchVisible)
        {
            StartClockTimer();
        }
    }

    private void OnOsdActivated(object? sender, EventArgs e)
    {
        // Akıllı/Fareyle modunda çentik gizli olabilir; gösterge süresince (3 sn) görünür kalsın.
        _visibility.OnMediaActivity();
        UpdateTargetSize();
    }

    private void OnOsdStateChanged(object? sender, EventArgs e)
    {
        UpdateTargetSize();
    }

    private void OnStatusPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StatusViewModel.BatteryPercent) or nameof(StatusViewModel.IsCharging) or nameof(StatusViewModel.HasBattery))
        {
            UpdateBatteryState();
        }
    }

    private void UpdateBatteryState()
    {
        BatteryPercent = _status.BatteryPercent;
        IsCharging = _status.IsCharging;
        HasBattery = _status.HasBattery;
        var pct = Math.Clamp(BatteryPercent, 0, 100);
        // Dış gövde 22 DIP, iç dolgu alanı 17 DIP (22 - 3 - 2)
        BatteryFillWidth = Math.Max(2.0, 17.0 * pct / 100.0);
    }

    private void StartClockTimer()
    {
        if (_clockTimer is not null)
            return;

        UpdateClock();
        _clockTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
    }

    private void StopClockTimer()
    {
        if (_clockTimer is not null)
        {
            _clockTimer.Stop();
            _clockTimer = null;
        }
    }

    private void UpdateClock()
    {
        ClockTime = DateTime.Now.ToString("HH:mm");
    }

    private void OnCalendarTimerFinished()
    {
        _controller.OnTrackChanged(); // Kısa süre Peek moduna geçirir
        ShakeRequested?.Invoke();
    }

    private void OnPanelTransitionTriggered(int direction)
    {
        PanelTransitionTriggered?.Invoke(direction);
    }

    private void OnMusicPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MusicViewModel.IsPlaying) or nameof(MusicViewModel.HasTrack))
        {
            OnPropertyChanged(nameof(IsMusicPlaying));
            if (State == NotchState.Collapsed)
                UpdateTargetSize();
        }
    }

    private void OnTrackChanged()
    {
        _controller.OnTrackChanged();
        _visibility.OnMediaActivity();
    }

    // Core zamanlayıcıları (otomatik peek, fare çıkışı, medya peek) ThreadPool'dan tikler;
    // durum ve pencere nesnelerine yalnız UI thread'inden dokunulur.
    private void RunOnUi(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.InvokeAsync(() =>
        {
            if (!_disposed)
                action();
        });
    }

    private void OnStateChanged(object? sender, NotchState newState)
    {
        RunOnUi(() => ApplyState(newState));
    }

    private void ApplyState(NotchState newState)
    {
        State = newState;
        UpdateTargetSize();
        UpdateActivePanels();
        _visibility.SetExpanded(State == NotchState.Expanded);
    }

    private void OnPanelChanged(object? sender, NotchPanel newPanel)
    {
        foreach (var tab in Tabs)
            tab.IsSelected = tab.Panel == newPanel;

        CurrentPanel = newPanel;
        OnPropertyChanged(nameof(CurrentPanelContent));
        UpdateTargetSize();
        UpdateActivePanels();
    }

    private void UpdateActivePanels()
    {
        var isExpanded = State == NotchState.Expanded;
        _music.SetActive(isExpanded && CurrentPanel == NotchPanel.Music);
        _status.SetActive(isExpanded && CurrentPanel == NotchPanel.Status);
        _controlCenter.SetActive(isExpanded && CurrentPanel == NotchPanel.ControlCenter);
    }

    private void OnVisibilityChanged(object? sender, VisibilityResult result)
    {
        RunOnUi(() => ApplyVisibility(result));
    }

    private void ApplyVisibility(VisibilityResult result)
    {
        IsNotchVisible = result.IsVisible;
        if (IsNotchVisible)
            StartClockTimer();
        else
            StopClockTimer();

        TargetVisibilityChanged?.Invoke(IsNotchVisible);
    }

    public void UpdateTargetSize()
    {
        if (Osd.IsActive)
        {
            TargetWidth = 260;
            TargetHeight = 40;
            TargetCornerRadius = 20;
        }
        else
        {
            (TargetWidth, TargetHeight, TargetCornerRadius) = State switch
            {
                NotchState.Collapsed => (IsMusicPlaying ? 280 : 236, 34, 18),
                NotchState.Peek => (300, 38, 20),
                // +30 DIP: üstteki sekme satırı. Sabit tuval 260 DIP olduğu için en yüksek panel de sığar.
                NotchState.Expanded => (420, CurrentPanel == NotchPanel.Music ? 142 : 170, 28),
                _ => (236, 34, 18)
            };
        }

        TargetSizeChanged?.Invoke(TargetWidth, TargetHeight);
    }

    public void ShowVolume(float volume, bool isMuted) => Osd.ShowVolume(volume, isMuted);
    public void ShowBrightness(int brightness) => Osd.ShowBrightness(brightness);

    public void ExpandToShelf()
    {
        _controller.Expand(NotchPanel.Shelf);
    }

    [RelayCommand]
    private void NextPanel() => _controller.NextPanel();

    [RelayCommand]
    private void SelectTab(NotchPanel panel) => _controller.GoToPanel(panel);

    /// <summary>Kapalı çentikteki Wi-Fi / Bluetooth simgesine tıklanınca çentik Kontrol sekmesinde açılır.</summary>
    [RelayCommand]
    private void OpenControlCenter() => _controller.Expand(NotchPanel.ControlCenter);

    private static string TabLabel(NotchPanel panel) => panel switch
    {
        NotchPanel.Music => Strings.Get("TabMusic"),
        NotchPanel.ControlCenter => Strings.Get("TabControlCenter"),
        NotchPanel.Status => Strings.Get("TabStatus"),
        NotchPanel.Calendar => Strings.Get("TabCalendar"),
        NotchPanel.Shelf => Strings.Get("TabShelf"),
        _ => panel.ToString()
    };

    [RelayCommand]
    private void PreviousPanel() => _controller.PreviousPanel();

    [RelayCommand]
    private void OpenSettings()
    {
        KoyWindow.ShowInstance(1);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        StopClockTimer();
        _controller.StateChanged -= OnStateChanged;
        _controller.PanelChanged -= OnPanelChanged;
        _controller.PanelTransitionTriggered -= OnPanelTransitionTriggered;
        _visibility.Changed -= OnVisibilityChanged;
        _music.TrackChanged -= OnTrackChanged;
        _music.PropertyChanged -= OnMusicPropertyChanged;
        _calendar.TimerFinished -= OnCalendarTimerFinished;
        _status.PropertyChanged -= OnStatusPropertyChanged;
        Osd.Activated -= OnOsdActivated;
        Osd.Dismissed -= OnOsdStateChanged;
        Osd.Dispose();
        _controller.Dispose();
        _visibility.Dispose();
        _music.Dispose();
        _controlCenter.Dispose();
        _status.Dispose();
        _calendar.Dispose();
        _shelf.Dispose();
    }
}

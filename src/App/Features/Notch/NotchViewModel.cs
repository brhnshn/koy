using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Features.Calendar;
using Koy.App.Features.ControlCenter;
using Koy.App.Features.Music;
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
    private bool _disposed;

    public AppSettings Settings { get; }
    public NotchController Controller => _controller;
    public MusicViewModel Music => _music;
    public ControlCenterViewModel ControlCenter => _controlCenter;
    public StatusViewModel Status => _status;
    public CalendarViewModel Calendar => _calendar;
    public ShelfViewModel Shelf => _shelf;
    public VisibilityController Visibility => _visibility;

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
    private double _targetWidth = 200;

    [ObservableProperty]
    private double _targetHeight = 34;

    [ObservableProperty]
    private bool _isNotchVisible = true;

    [ObservableProperty]
    private string _title = "Koy";

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
        VisibilityController? visibility = null)
    {
        Settings = settings;
        _controller = controller;
        _music = music;
        _controlCenter = controlCenter;
        _status = status;
        _calendar = calendar;
        _shelf = shelf;
        _visibility = visibility ?? new VisibilityController(settings.NotchVisibility);

        _isNotchVisible = _visibility.IsVisible;

        _controller.StateChanged += OnStateChanged;
        _controller.PanelChanged += OnPanelChanged;
        _controller.PanelTransitionTriggered += OnPanelTransitionTriggered;
        _visibility.Changed += OnVisibilityChanged;
        _music.TrackChanged += OnTrackChanged;
        _music.PropertyChanged += OnMusicPropertyChanged;
        _calendar.TimerFinished += OnCalendarTimerFinished;

        UpdateTargetSize();
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
            if (State == NotchState.Collapsed)
                UpdateTargetSize();
        }
    }

    private void OnTrackChanged()
    {
        _controller.OnTrackChanged();
        _visibility.OnMediaActivity();
    }

    private void OnStateChanged(object? sender, NotchState newState)
    {
        State = newState;
        UpdateTargetSize();
        UpdateActivePanels();
        _visibility.SetExpanded(State == NotchState.Expanded);
    }

    private void OnPanelChanged(object? sender, NotchPanel newPanel)
    {
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
    }

    private void OnVisibilityChanged(object? sender, VisibilityResult result)
    {
        IsNotchVisible = result.IsVisible;
        TargetVisibilityChanged?.Invoke(IsNotchVisible);
    }

    public void UpdateTargetSize()
    {
        (TargetWidth, TargetHeight) = State switch
        {
            NotchState.Collapsed => (Music.HasTrack && Music.IsPlaying ? 220 : 200, 34),
            NotchState.Peek => (300, 38),
            NotchState.Expanded => (420, 140),
            _ => (200, 34)
        };

        TargetSizeChanged?.Invoke(TargetWidth, TargetHeight);
    }

    [RelayCommand]
    private void NextPanel() => _controller.NextPanel();

    [RelayCommand]
    private void PreviousPanel() => _controller.PreviousPanel();

    [RelayCommand]
    private void OpenSettings()
    {
        var store = new SettingsStore(SettingsStore.DefaultPath);
        var win = new SettingsWindow
        {
            DataContext = new SettingsViewModel(store)
        };
        if (win.ShowDialog() == true)
        {
            var updated = store.Load();
            _visibility.Mode = updated.NotchVisibility;
            _controller.SetEnabledPanels(NotchFeature.ParseEnabledPanels(updated.EnabledPanels));
        }
    }

    [RelayCommand]
    private static void Quit() => System.Windows.Application.Current.Shutdown();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _controller.StateChanged -= OnStateChanged;
        _controller.PanelChanged -= OnPanelChanged;
        _controller.PanelTransitionTriggered -= OnPanelTransitionTriggered;
        _visibility.Changed -= OnVisibilityChanged;
        _music.TrackChanged -= OnTrackChanged;
        _music.PropertyChanged -= OnMusicPropertyChanged;
        _calendar.TimerFinished -= OnCalendarTimerFinished;
        _controller.Dispose();
        _visibility.Dispose();
        _music.Dispose();
        _controlCenter.Dispose();
        _status.Dispose();
        _calendar.Dispose();
        _shelf.Dispose();
    }
}

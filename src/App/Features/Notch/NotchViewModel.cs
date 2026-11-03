using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Features.Music;
using Koy.Core.Notch;
using Koy.Core.Settings;
using Koy.Core.Visibility;

namespace Koy.App.Features.Notch;

public sealed partial class NotchViewModel : ObservableObject, IDisposable
{
    private readonly NotchController _controller;
    private readonly MusicViewModel _music;
    private readonly VisibilityController _visibility;
    private bool _disposed;

    public AppSettings Settings { get; }
    public NotchController Controller => _controller;
    public MusicViewModel Music => _music;
    public VisibilityController Visibility => _visibility;

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

    public NotchViewModel(
        AppSettings settings,
        NotchController controller,
        MusicViewModel music,
        VisibilityController? visibility = null)
    {
        Settings = settings;
        _controller = controller;
        _music = music;
        _visibility = visibility ?? new VisibilityController(settings.NotchVisibility);

        _isNotchVisible = _visibility.IsVisible;

        _controller.StateChanged += OnStateChanged;
        _controller.PanelChanged += OnPanelChanged;
        _visibility.Changed += OnVisibilityChanged;
        _music.TrackChanged += OnTrackChanged;
        _music.PropertyChanged += OnMusicPropertyChanged;

        UpdateTargetSize();
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
        _music.SetActive(State == NotchState.Expanded && CurrentPanel == NotchPanel.Music);
        _visibility.SetExpanded(State == NotchState.Expanded);
    }

    private void OnPanelChanged(object? sender, NotchPanel newPanel)
    {
        CurrentPanel = newPanel;
        UpdateTargetSize();
        _music.SetActive(State == NotchState.Expanded && CurrentPanel == NotchPanel.Music);
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
    private static void Quit() => System.Windows.Application.Current.Shutdown();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _controller.StateChanged -= OnStateChanged;
        _controller.PanelChanged -= OnPanelChanged;
        _visibility.Changed -= OnVisibilityChanged;
        _music.TrackChanged -= OnTrackChanged;
        _music.PropertyChanged -= OnMusicPropertyChanged;
        _controller.Dispose();
        _visibility.Dispose();
        _music.Dispose();
    }
}

using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Media;

namespace Koy.App.Features.Music;

/// <summary>
/// Müzik paneli ViewModel'i. IMediaService ile arayüz arasındaki köprüyü kurar.
/// Zamanlayıcı yalnız panel Expanded durumundayken çalışır (%0 boşta CPU).
/// </summary>
public sealed partial class MusicViewModel : ObservableObject, IDisposable
{
    private readonly IMediaService _mediaService;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _progressTimer;
    private bool _isActive;
    private bool _disposed;

    [ObservableProperty]
    private bool _hasTrack;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _artist = string.Empty;

    [ObservableProperty]
    private string _sourceAppId = string.Empty;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private ImageSource? _thumbnail;

    [ObservableProperty]
    private TimeSpan _position = TimeSpan.Zero;

    [ObservableProperty]
    private TimeSpan _duration = TimeSpan.Zero;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _positionText = "0:00";

    [ObservableProperty]
    private string _durationText = "0:00";

    [ObservableProperty]
    private IReadOnlyList<MediaSessionInfo> _sessions = [];

    public bool HasMultipleSessions => Sessions.Count > 1;
    public bool ShowCollapsedMedia => HasTrack && IsPlaying;

    public event Action? TrackChanged;

    public MusicViewModel(IMediaService mediaService, Dispatcher? dispatcher = null)
    {
        _mediaService = mediaService;
        _dispatcher = dispatcher ?? System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        _progressTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _progressTimer.Tick += OnProgressTick;

        _mediaService.Changed += OnMediaChanged;
        _mediaService.TimelineChanged += OnTimelineChanged;
        _mediaService.SessionsChanged += OnSessionsChanged;

        if (_mediaService.Current is not null)
            ApplyMediaInfo(_mediaService.Current);

        if (_mediaService.Timeline is not null)
            ApplyTimeline(_mediaService.Timeline);

        Sessions = _mediaService.Sessions;
    }

    public void SetActive(bool active)
    {
        _isActive = active;
        UpdateProgressTimer();
    }

    private void UpdateProgressTimer()
    {
        if (_isActive && HasTrack && IsPlaying)
        {
            if (!_progressTimer.IsEnabled)
                _progressTimer.Start();
        }
        else
        {
            if (_progressTimer.IsEnabled)
                _progressTimer.Stop();
        }
    }

    [RelayCommand]
    private async Task PlayPause() => await _mediaService.PlayPauseAsync();

    [RelayCommand]
    private async Task Next() => await _mediaService.NextAsync();

    [RelayCommand]
    private async Task Previous() => await _mediaService.PreviousAsync();

    [RelayCommand]
    private async Task Seek(double progressFraction)
    {
        if (Duration > TimeSpan.Zero)
        {
            var clamped = Math.Clamp(progressFraction, 0.0, 1.0);
            var target = TimeSpan.FromTicks((long)(Duration.Ticks * clamped));
            Position = target;
            Progress = clamped;
            PositionText = FormatTime(target);
            await _mediaService.SeekAsync(target);
        }
    }

    [RelayCommand]
    private async Task SelectSession(string sessionId) => await _mediaService.SelectSessionAsync(sessionId);

    private void OnMediaChanged(object? sender, MediaInfo? info)
    {
        _dispatcher.InvokeAsync(() =>
        {
            var oldTitle = Title;
            ApplyMediaInfo(info);
            if (HasTrack && oldTitle != Title)
                TrackChanged?.Invoke();
        });
    }

    private void OnTimelineChanged(object? sender, PlaybackTimeline? timeline)
    {
        _dispatcher.InvokeAsync(() => ApplyTimeline(timeline));
    }

    private void OnSessionsChanged(object? sender, IReadOnlyList<MediaSessionInfo> sessions)
    {
        _dispatcher.InvokeAsync(() =>
        {
            Sessions = sessions;
            OnPropertyChanged(nameof(HasMultipleSessions));
        });
    }

    private void ApplyMediaInfo(MediaInfo? info)
    {
        if (info is null || string.IsNullOrWhiteSpace(info.Title))
        {
            HasTrack = false;
            Title = string.Empty;
            Artist = string.Empty;
            SourceAppId = string.Empty;
            IsPlaying = false;
            Thumbnail = null;
            UpdateProgressTimer();
            OnPropertyChanged(nameof(ShowCollapsedMedia));
            return;
        }

        HasTrack = true;
        Title = info.Title;
        Artist = info.Artist;
        SourceAppId = MediaAppName.FromSourceAppId(info.SourceAppId);
        IsPlaying = info.IsPlaying;
        Thumbnail = DecodeThumbnail(info.Thumbnail);

        UpdateProgressTimer();
        OnPropertyChanged(nameof(ShowCollapsedMedia));
    }

    private void ApplyTimeline(PlaybackTimeline? timeline)
    {
        if (timeline is null)
        {
            Position = TimeSpan.Zero;
            Duration = TimeSpan.Zero;
            Progress = 0;
            PositionText = "0:00";
            DurationText = "0:00";
            return;
        }

        Duration = timeline.EndTime - timeline.StartTime;
        if (Duration < TimeSpan.Zero)
            Duration = TimeSpan.Zero;
        var rawPos = timeline.Position - timeline.StartTime;

        // Çalma sürüyorsa geçen süreyi ekle
        if (IsPlaying && timeline.LastUpdated != DateTimeOffset.MinValue)
        {
            var elapsed = DateTimeOffset.Now - timeline.LastUpdated;
            if (elapsed > TimeSpan.Zero && elapsed < TimeSpan.FromHours(1))
                rawPos += elapsed;
        }

        Position = rawPos < TimeSpan.Zero ? TimeSpan.Zero : (rawPos > Duration ? Duration : rawPos);
        Progress = Duration.TotalSeconds > 0 ? Math.Clamp(Position.TotalSeconds / Duration.TotalSeconds, 0.0, 1.0) : 0.0;
        PositionText = FormatTime(Position);
        DurationText = FormatTime(Duration);
    }

    private void OnProgressTick(object? sender, EventArgs e)
    {
        if (!IsPlaying || Duration <= TimeSpan.Zero)
            return;

        var nextPos = Position + TimeSpan.FromSeconds(1);
        if (nextPos > Duration)
            nextPos = Duration;

        Position = nextPos;
        Progress = Math.Clamp(Position.TotalSeconds / Duration.TotalSeconds, 0.0, 1.0);
        PositionText = FormatTime(Position);
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes}:{time.Seconds:D2}";
    }

    private static BitmapImage? DecodeThumbnail(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.DecodePixelWidth = 96;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(bytes);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _progressTimer.Stop();
        _progressTimer.Tick -= OnProgressTick;
        _mediaService.Changed -= OnMediaChanged;
        _mediaService.TimelineChanged -= OnTimelineChanged;
        _mediaService.SessionsChanged -= OnSessionsChanged;
    }
}

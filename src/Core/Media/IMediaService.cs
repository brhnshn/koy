namespace Koy.Core.Media;

/// <summary>
/// Medya servisi sözleşmesi. Core'da yer alır, WinRT/Win32 tipleri içermez.
/// Uygulaması Platform projesindedir (GSMTC tabanlı).
/// </summary>
public interface IMediaService : IDisposable
{
    event EventHandler<MediaInfo?>? Changed;
    event EventHandler<PlaybackTimeline?>? TimelineChanged;
    event EventHandler<IReadOnlyList<MediaSessionInfo>>? SessionsChanged;

    MediaInfo? Current { get; }
    PlaybackTimeline? Timeline { get; }
    IReadOnlyList<MediaSessionInfo> Sessions { get; }

    Task StartAsync();
    void Stop();

    Task PlayPauseAsync();
    Task NextAsync();
    Task PreviousAsync();
    Task SeekAsync(TimeSpan position);
    Task SelectSessionAsync(string sessionId);
}

public sealed record MediaInfo(
    string Title,
    string Artist,
    string SourceAppId,
    bool IsPlaying,
    byte[]? Thumbnail);

public sealed record PlaybackTimeline(
    TimeSpan Position,
    TimeSpan StartTime,
    TimeSpan EndTime,
    DateTimeOffset LastUpdated);

public sealed record MediaSessionInfo(
    string Id,
    string SourceAppId,
    string Title,
    bool IsCurrent);

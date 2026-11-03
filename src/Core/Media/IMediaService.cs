namespace Koy.Core.Media;

/// <summary>
/// Sözleşme Core'da, uygulaması Platform'da (M1: GlobalSystemMediaTransportControlsSessionManager).
/// Burada WinRT tipi kullanılmaz; kapak resmi gibi veriler düz .NET tipleriyle taşınır.
/// </summary>
public interface IMediaService : IDisposable
{
    event EventHandler<MediaInfo?>? Changed;

    MediaInfo? Current { get; }

    Task StartAsync();

    void Stop();

    Task PlayPauseAsync();

    Task NextAsync();

    Task PreviousAsync();
}

public sealed record MediaInfo(
    string Title,
    string Artist,
    string SourceAppId,
    bool IsPlaying,
    byte[]? Thumbnail);

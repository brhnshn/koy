namespace Koy.Core.Audio;

/// <summary>
/// Sistem ses seviyesi ve sessize alma sözleşmesi.
/// Saf C#: Platform uygulaması CoreAudio (IAudioEndpointVolume) ile çalışır.
/// </summary>
public interface IAudioService : IDisposable
{
    float Volume { get; }
    bool IsMuted { get; }

    event EventHandler<AudioStateChangedEventArgs>? StateChanged;

    void SetVolume(float volume);
    void SetMute(bool isMuted);
    void ToggleMute();
}

public sealed record AudioStateChangedEventArgs(float Volume, bool IsMuted);

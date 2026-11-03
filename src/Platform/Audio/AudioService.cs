using System.Runtime.InteropServices;
using Koy.Core.Audio;
using Windows.Win32;
using Windows.Win32.Media.Audio;
using Windows.Win32.Media.Audio.Endpoints;
using Windows.Win32.System.Com;

namespace Koy.Platform.Audio;

public sealed class AudioService : IAudioService
{
    private static readonly Guid ClsidMMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IidIAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;

    private IAudioEndpointVolume? _endpointVolume;
    private VolumeCallback? _callback;
    private bool _disposed;

    public float Volume { get; private set; }
    public bool IsMuted { get; private set; }

    public event EventHandler<AudioStateChangedEventArgs>? StateChanged;

    public AudioService()
    {
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(ClsidMMDeviceEnumerator);
            if (enumeratorType is null)
                return;

            var enumeratorObj = Activator.CreateInstance(enumeratorType);
            if (enumeratorObj is not IMMDeviceEnumerator enumerator)
                return;

            try
            {
                enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
                if (device is null)
                    return;

                try
                {
                    var iid = IidIAudioEndpointVolume;
                    unsafe
                    {
                        device.Activate(&iid, CLSCTX.CLSCTX_ALL, null, out var obj);
                        _endpointVolume = obj as IAudioEndpointVolume;
                    }

                    if (_endpointVolume is not null)
                    {
                        _callback = new VolumeCallback(this);
                        _endpointVolume.RegisterControlChangeNotify(_callback);
                        RefreshState();
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(device);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(enumerator);
            }
        }
        catch
        {
            // Ses aygıtı yoksa veya COM hatası durumunda çökme
        }
    }

    public void RefreshState()
    {
        if (_endpointVolume is null)
            return;

        try
        {
            _endpointVolume.GetMasterVolumeLevelScalar(out var level);
            _endpointVolume.GetMute(out var muted);

            Volume = level;
            IsMuted = muted;

            StateChanged?.Invoke(this, new AudioStateChangedEventArgs(Volume, IsMuted));
        }
        catch
        {
        }
    }

    public void SetVolume(float volume)
    {
        if (_endpointVolume is null)
            return;

        try
        {
            var clamped = Math.Clamp(volume, 0f, 1f);
            _endpointVolume.SetMasterVolumeLevelScalar(clamped, Guid.Empty);
            Volume = clamped;
            StateChanged?.Invoke(this, new AudioStateChangedEventArgs(Volume, IsMuted));
        }
        catch
        {
        }
    }

    public void SetMute(bool isMuted)
    {
        if (_endpointVolume is null)
            return;

        try
        {
            _endpointVolume.SetMute(isMuted, Guid.Empty);
            IsMuted = isMuted;
            StateChanged?.Invoke(this, new AudioStateChangedEventArgs(Volume, IsMuted));
        }
        catch
        {
        }
    }

    public void ToggleMute()
    {
        SetMute(!IsMuted);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_endpointVolume is not null && _callback is not null)
        {
            try
            {
                _endpointVolume.UnregisterControlChangeNotify(_callback);
            }
            catch
            {
            }
        }

        if (_endpointVolume is not null)
        {
            Marshal.ReleaseComObject(_endpointVolume);
            _endpointVolume = null;
        }

        _callback = null;
    }

    private sealed class VolumeCallback(AudioService service) : IAudioEndpointVolumeCallback
    {
        public unsafe void OnNotify(AUDIO_VOLUME_NOTIFICATION_DATA* pNotify)
        {
            if (pNotify != null)
            {
                service.RefreshState();
            }
        }
    }
}

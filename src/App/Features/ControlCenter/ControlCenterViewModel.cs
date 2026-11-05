using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.Core.Audio;
using Koy.Core.Diagnostics;
using Koy.Core.Display;
using Koy.Core.Radio;
using Koy.Core.Settings;
using Koy.Core.Shell;

namespace Koy.App.Features.ControlCenter;

public sealed partial class ControlCenterViewModel : ObservableObject, IDisposable
{
    private readonly Func<IAudioService> _audioFactory;
    private readonly Func<IRadioService> _radioFactory;
    private readonly Action? _radioRelease;
    private readonly Func<IBrightnessService> _brightnessFactory;
    private readonly IFocusAssistService _focusAssistService;
    private IAudioService? _audioService;
    private IRadioService? _radioService;
    private IBrightnessService? _brightnessService;
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;
    private bool _brightnessStarted;
    private bool _disposed;

    // Son açma/kapama denemesi reddedildiyse döşeme bunu yazar; radyo durumu değişince silinir.
    private readonly Dictionary<RadioKind, bool> _toggleFailed = new() { [RadioKind.WiFi] = false, [RadioKind.Bluetooth] = false };

    public RadioTileViewModel WiFi { get; } = new(RadioKind.WiFi);

    public RadioTileViewModel Bluetooth { get; } = new(RadioKind.Bluetooth);

    [ObservableProperty]
    private float _volume;

    [ObservableProperty]
    private int _volumePercent;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private int _brightness;

    [ObservableProperty]
    private bool _isBrightnessSupported;

    // Servisler panel ilk açıldığında kurulur (AGENTS.md #5): WMI parlaklık sorgusu, CoreAudio ve
    // radyo erişimi açılışta birkaç MB tutuyor ve panel hiç açılmayabilir. Kurulduktan sonra
    // notch kapanana kadar yaşar ve bu ViewModel tarafından bırakılır. İstisna: radyo servisi
    // (kapalı çentikteki Wi-Fi / Bluetooth simgeleri için) çentik açılırken kurulur ve bütün monitörlerde
    // paylaşılır; radioRelease verilirse paylaşımlı sayaç azaltılır, verilmezse servis burada Dispose edilir.
    public ControlCenterViewModel(
        Func<IAudioService> audioFactory,
        Func<IRadioService> radioFactory,
        Action? radioRelease,
        Func<IBrightnessService> brightnessFactory,
        IFocusAssistService focusAssistService,
        AppSettings settings,
        Dispatcher? dispatcher = null)
    {
        _audioFactory = audioFactory;
        _radioFactory = radioFactory;
        _radioRelease = radioRelease;
        _brightnessFactory = brightnessFactory;
        _focusAssistService = focusAssistService;
        _settings = settings;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    /// <summary>
    /// Kapalı çentikteki Wi-Fi / Bluetooth simgeleri için radyoları panel açılmadan izlemeye başlar.
    /// Durum Radio.StateChanged olayıyla gelir; zamanlayıcı ya da yoklama yok.
    /// </summary>
    public void StartRadioStatus()
    {
        if (_disposed || _radioService is not null)
            return;

        var created = EnsureRadioService();

        // Paylaşımlı servis başka bir monitörde zaten tarandıysa yeniden taranmaz: durum EnsureRadioService'te alındı.
        if (created && _radioService!.WiFiState == RadioState.Unknown && _radioService.BluetoothState == RadioState.Unknown)
            Observe(_radioService.RefreshAsync(), "ControlCenter radyo izleme");
    }

    // Servis yeni bağlandıysa true. Paylaşımlı servis durumu zaten biliyor olabilir (olay bir daha gelmez),
    // bu yüzden döşemeler mevcut durumdan hemen beslenir.
    private bool EnsureRadioService()
    {
        if (_radioService is not null)
            return false;

        _radioService = _radioFactory();
        _radioService.StateChanged += OnRadioStateChanged;
        UpdateRadioTile(RadioKind.WiFi);
        UpdateRadioTile(RadioKind.Bluetooth);
        return true;
    }

    public void SetActive(bool active)
    {
        if (!active || _disposed)
            return;

        EnsureRadioService();

        // Her açılışta yeniden taranır: USB Wi-Fi takılıp çıkarılmış ya da radyo izni ayarlardan
        // sonradan verilmiş olabilir. Yalnız tek seferlik sorgu; zamanlayıcı yok.
        Observe(_radioService!.RefreshAsync(), "ControlCenter radyo taraması");

        EnsureAudioService();

        // Ses tekerlekle önceden kurulmuş olabilir; parlaklık yine de ilk açılışta bir kez başlar.
        if (_brightnessStarted)
            return;

        _brightnessStarted = true;
        Observe(InitializeBrightnessAsync(), "ControlCenter parlaklık başlatma");
    }

    // Fire-and-forget görevlerin istisnası yutulmasın: gözlenir ve günlüğe yazılır.
    private static void Observe(Task task, string source) =>
        _ = task.ContinueWith(
            t => CrashLog.Write(source, t.Exception),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

    private void EnsureAudioService()
    {
        if (_audioService is not null || _disposed)
            return;

        _audioService = _audioFactory();
        _audioService.StateChanged += OnAudioStateChanged;
        Volume = _audioService.Volume;
        VolumePercent = (int)Math.Round(Volume * 100);
        IsMuted = _audioService.IsMuted;
    }

    /// <summary>Çentik tekerlek eylemi "Ses" iken: panel açılmadan sistem sesini yüzde olarak artırır ya da azaltır.</summary>
    public void AdjustVolume(int deltaPercent)
    {
        EnsureAudioService();
        if (_audioService is null)
            return;

        var current = (int)Math.Round(_audioService.Volume * 100);
        OnVolumeSliderChanged(Math.Clamp(current + deltaPercent, 0, 100));
    }

    private async Task InitializeBrightnessAsync()
    {
        // WMI sorgusu UI iş parçacığını bekletmesin.
        var service = await Task.Run(_brightnessFactory);
        if (_disposed)
        {
            service.Dispose();
            return;
        }

        _brightnessService = service;
        _brightnessService.BrightnessChanged += OnBrightnessChanged;
        IsBrightnessSupported = _brightnessService.IsSupported;
        Brightness = _brightnessService.Brightness;
    }

    private void OnAudioStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            Volume = e.Volume;
            VolumePercent = (int)Math.Round(e.Volume * 100);
            IsMuted = e.IsMuted;
        });
    }

    private void OnRadioStateChanged(object? sender, RadioStateChangedEventArgs e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            var tile = GetTile(e.Kind);
            if (tile.State.IsOn != (e.State == RadioState.On))
                _toggleFailed[e.Kind] = false;

            UpdateRadioTile(e.Kind);
        });
    }

    private RadioTileViewModel GetTile(RadioKind kind) => kind == RadioKind.WiFi ? WiFi : Bluetooth;

    private void UpdateRadioTile(RadioKind kind)
    {
        if (_radioService is null)
            return;

        var state = kind == RadioKind.WiFi ? _radioService.WiFiState : _radioService.BluetoothState;
        GetTile(kind).Apply(RadioTileResolver.Resolve(_radioService.Access, state, _toggleFailed[kind]));
    }

    private void OnBrightnessChanged(object? sender, int e)
    {
        _dispatcher.InvokeAsync(() =>
        {
            Brightness = e;
        });
    }

    /// <summary>
    /// Döşemeye tıklama: radyo değiştirilebiliyorsa açar/kapatır, değiştirilemiyorsa nedenine göre
    /// ilgili Windows ayarını açar (sahte anahtar yok).
    /// </summary>
    [RelayCommand]
    private async Task ActivateRadioAsync(RadioKind kind)
    {
        if (_radioService is null)
            return;

        switch (GetTile(kind).State.Action)
        {
            case RadioTileAction.Toggle:
                var ok = await _radioService.ToggleAsync(kind);
                _toggleFailed[kind] = !ok;
                UpdateRadioTile(kind);
                break;
            case RadioTileAction.OpenSettings:
                _radioService.OpenSettings(kind);
                break;
            case RadioTileAction.OpenPrivacySettings:
                _radioService.OpenPrivacySettings();
                break;
        }
    }

    /// <summary>Ağ listesi ve cihaz eşleştirme için Windows'un kendi sayfası.</summary>
    [RelayCommand]
    private void OpenRadioSettings(RadioKind kind) => _radioService?.OpenSettings(kind);

    /// <summary>
    /// Rahatsız Etme'yi açıp kapatan ve durumunu güvenilir okuyan resmi bir API yok; döşeme yalnız
    /// Windows'taki ayarı açar ve açık/kapalı durumu uydurmaz.
    /// </summary>
    [RelayCommand]
    private void OpenDoNotDisturbSettings() => _focusAssistService.OpenSettings();

    [RelayCommand]
    private void ToggleMute()
    {
        _audioService?.ToggleMute();
    }

    public void OnVolumeSliderChanged(double newVolumePercent)
    {
        var normalized = (float)Math.Clamp(newVolumePercent / 100.0, 0.0, 1.0);
        _audioService?.SetVolume(normalized);
        Volume = normalized;
        VolumePercent = (int)Math.Round(newVolumePercent);
    }

    public void OnBrightnessSliderChanged(double newBrightness)
    {
        var intBrightness = (int)Math.Clamp(newBrightness, 0, 100);
        _brightnessService?.SetBrightness(intBrightness);
        Brightness = intBrightness;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_audioService is not null)
        {
            _audioService.StateChanged -= OnAudioStateChanged;
            _audioService.Dispose();
            _audioService = null;
        }

        if (_radioService is not null)
        {
            _radioService.StateChanged -= OnRadioStateChanged;
            if (_radioRelease is not null)
                _radioRelease();
            else
                _radioService.Dispose();
            _radioService = null;
        }

        if (_brightnessService is not null)
        {
            _brightnessService.BrightnessChanged -= OnBrightnessChanged;
            _brightnessService.Dispose();
            _brightnessService = null;
        }
    }
}

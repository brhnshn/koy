using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Koy.Core.Audio;
using Koy.Core.Display;

namespace Koy.App.Features.Notch.OSD;

public enum OsdType
{
    None,
    Volume,
    Brightness
}

/// <summary>
/// Çentik OSD (ses ve parlaklık HUD göstergesi) görünüm modeli.
/// 260×40 px ebadında, 1.5 saniye hareketsizlik sonrası otomatik kapanan gösterge mantığı.
/// </summary>
public sealed partial class OsdIndicatorViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _autoDismissTimer;
    private IAudioService? _attachedAudio;
    private IBrightnessService? _attachedBrightness;
    private bool _disposed;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private OsdType _type = OsdType.None;

    [ObservableProperty]
    private int _percent;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private string _percentText = "";

    public GridLength FillStarWidth => new(Math.Clamp(Percent, 0, 100), GridUnitType.Star);
    public GridLength RemainingStarWidth => new(Math.Max(0, 100 - Math.Clamp(Percent, 0, 100)), GridUnitType.Star);

    public event EventHandler? Activated;
    public event EventHandler? Dismissed;

    public OsdIndicatorViewModel(Dispatcher? dispatcher = null)
    {
        var disp = dispatcher ?? (Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher);
        _autoDismissTimer = new DispatcherTimer(DispatcherPriority.Normal, disp)
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };
        _autoDismissTimer.Tick += OnAutoDismissTick;
    }

    /// <summary>
    /// Ses seviyesi ve sessize alma durumunu gösterir (PROJECT.md OSD kontratı).
    /// </summary>
    public void ShowVolume(float volume, bool isMuted)
    {
        var pct = (int)Math.Round(Math.Clamp(volume, 0f, 1f) * 100);
        Type = OsdType.Volume;
        Percent = pct;
        IsMuted = isMuted;
        PercentText = pct.ToString();
        IsActive = true;

        OnPropertyChanged(nameof(FillStarWidth));
        OnPropertyChanged(nameof(RemainingStarWidth));

        ResetTimer();
        Activated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Ekran parlaklık seviyesini gösterir (PROJECT.md OSD kontratı).
    /// </summary>
    public void ShowBrightness(int brightness)
    {
        var pct = Math.Clamp(brightness, 0, 100);
        Type = OsdType.Brightness;
        Percent = pct;
        IsMuted = false;
        PercentText = pct.ToString();
        IsActive = true;

        OnPropertyChanged(nameof(FillStarWidth));
        OnPropertyChanged(nameof(RemainingStarWidth));

        ResetTimer();
        Activated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// İsteğe bağlı olarak harici IAudioService dinlemesi bağlar.
    /// </summary>
    public void Attach(IAudioService audioService)
    {
        DetachAudio();
        _attachedAudio = audioService;
        _attachedAudio.StateChanged += OnAudioStateChanged;
    }

    /// <summary>
    /// İsteğe bağlı olarak harici IBrightnessService dinlemesi bağlar.
    /// </summary>
    public void Attach(IBrightnessService brightnessService)
    {
        DetachBrightness();
        _attachedBrightness = brightnessService;
        _attachedBrightness.BrightnessChanged += OnBrightnessChanged;
    }

    private void OnAudioStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        var disp = _autoDismissTimer.Dispatcher;
        if (disp.CheckAccess())
        {
            ShowVolume(e.Volume, e.IsMuted);
        }
        else
        {
            disp.InvokeAsync(() => ShowVolume(e.Volume, e.IsMuted));
        }
    }

    private void OnBrightnessChanged(object? sender, int e)
    {
        var disp = _autoDismissTimer.Dispatcher;
        if (disp.CheckAccess())
        {
            ShowBrightness(e);
        }
        else
        {
            disp.InvokeAsync(() => ShowBrightness(e));
        }
    }

    private void ResetTimer()
    {
        _autoDismissTimer.Stop();
        _autoDismissTimer.Start();
    }

    private void OnAutoDismissTick(object? sender, EventArgs e)
    {
        _autoDismissTimer.Stop();
        Dismiss();
    }

    public void Dismiss()
    {
        if (!IsActive)
            return;

        IsActive = false;
        Type = OsdType.None;
        Dismissed?.Invoke(this, EventArgs.Empty);
    }

    private void DetachAudio()
    {
        if (_attachedAudio is not null)
        {
            _attachedAudio.StateChanged -= OnAudioStateChanged;
            _attachedAudio = null;
        }
    }

    private void DetachBrightness()
    {
        if (_attachedBrightness is not null)
        {
            _attachedBrightness.BrightnessChanged -= OnBrightnessChanged;
            _attachedBrightness = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _autoDismissTimer.Stop();
        _autoDismissTimer.Tick -= OnAutoDismissTick;
        DetachAudio();
        DetachBrightness();
    }
}

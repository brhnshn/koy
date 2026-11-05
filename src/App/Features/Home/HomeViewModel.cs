using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Koy.App.Features.Calendar;
using Koy.App.Features.Music;
using Koy.App.Features.Status;
using Koy.Core.Diagnostics;
using Koy.Core.Shell;

namespace Koy.App.Features.Home;

/// <summary>
/// Ana görünümün (müzik + durum sütunu) veri kaynağı: saat, tarih, sıradaki etkinlik ve zamanlayıcı özeti,
/// ağ, pil, hava durumu ve Bildirim Merkezi komutu. Yalnız mevcut ViewModel'leri birleştirir; görsel düzen
/// (XAML) tasarım onayından sonra bağlanır, o zamana kadar çentik düzeni değişmez.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject, IDisposable
{
    private readonly IShellActionService? _shell;
    private bool _disposed;

    public MusicViewModel Music { get; }
    public StatusViewModel Status { get; }
    public CalendarViewModel Calendar { get; }

    /// <summary>Sol sütunda müzik gösterilir (parça var).</summary>
    public bool ShowMusic => Music.HasTrack;

    /// <summary>Sol sütunda büyük saat, tam tarih ve sıradaki etkinlik gösterilir (parça yok).</summary>
    public bool ShowIdle => !Music.HasTrack;

    public HomeViewModel(MusicViewModel music, StatusViewModel status, CalendarViewModel calendar, IShellActionService? shell = null)
    {
        Music = music;
        Status = status;
        Calendar = calendar;
        _shell = shell;
        Music.PropertyChanged += OnMusicPropertyChanged;
    }

    private void OnMusicPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MusicViewModel.HasTrack))
        {
            OnPropertyChanged(nameof(ShowMusic));
            OnPropertyChanged(nameof(ShowIdle));
        }
    }

    /// <summary>Win+N ile Windows Bildirim Merkezi'ni açar (SendInput; <c>ms-actioncenter:</c> Hızlı Ayarlar'ı açar, bildirimleri değil).</summary>
    [RelayCommand]
    private void OpenNotificationCenter()
    {
        try
        {
            _shell?.OpenNotificationCenter();
        }
        catch (Exception ex)
        {
            CrashLog.Write("Bildirim Merkezi açılamadı", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Music.PropertyChanged -= OnMusicPropertyChanged;
    }
}

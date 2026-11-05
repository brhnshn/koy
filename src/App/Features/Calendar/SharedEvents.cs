using Koy.Core.Calendar;
using Koy.Core.Diagnostics;

namespace Koy.App.Features.Calendar;

/// <summary>
/// Yerel etkinlik defterinin tek örneği: bütün monitörlerdeki çentikler aynı listeyi görür. Dosya ilk ihtiyaçta
/// okunur (takvim ya da ana görünüm ilk açılınca), açılışta disk işi yapılmaz. Değişiklik hemen diske yazılır.
/// Yalnız arayüz iş parçacığından kullanılır.
/// </summary>
public sealed class SharedEvents
{
    /// <summary>Bitişi bu kadar günden eskiyse etkinlik silinir (liste şişmesin).</summary>
    private const int KeepDays = 60;

    private readonly EventStore _store;
    private EventBook? _book;

    public SharedEvents(EventStore? store = null)
    {
        _store = store ?? new EventStore(EventStore.DefaultPath);
    }

    /// <summary>Etkinlik eklendi ya da silindi (başka bir monitörden olsa da).</summary>
    public event EventHandler? Changed;

    public EventBook Book
    {
        get
        {
            if (_book is null)
            {
                _book = new EventBook(_store.Load());
                _book.PruneBefore(DateOnly.FromDateTime(DateTime.Today).AddDays(-KeepDays));
                _book.Changed += OnBookChanged;
            }

            return _book;
        }
    }

    private void OnBookChanged(object? sender, EventArgs e)
    {
        try
        {
            _store.Save(Book.Events);
        }
        catch (Exception ex)
        {
            // Disk dolu ya da dosya kilitli: etkinlik bu oturumda bellekte kalır, uygulama çökmez.
            CrashLog.Write("Takvim etkinlikleri kaydedilemedi", ex);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}

namespace Koy.Core.Dock;

/// <summary>
/// Sık gelen "yenile" isteklerini tek bir yenilemeye indirger. İlk istekten <c>delay</c> sonra eylem bir kez çalışır;
/// bu aralıktaki diğer istekler birleşir (fırtına sırasında hiç çalışmama riski yok: ilk istek süreyi belirler).
/// Eylem <c>post</c> ile (ör. UI dispatcher'ı) çalıştırılır; zamanlayıcı ThreadPool'dan tikler.
/// </summary>
public sealed class RefreshDebouncer : IDisposable
{
    private readonly Action _action;
    private readonly TimeSpan _delay;
    private readonly Action<Action>? _post;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private ITimer? _timer;
    private bool _pending;
    private bool _disposed;

    public RefreshDebouncer(Action action, TimeSpan delay, Action<Action>? post = null, TimeProvider? timeProvider = null)
    {
        _action = action;
        _delay = delay;
        _post = post;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool IsPending
    {
        get { lock (_gate) return _pending; }
    }

    public void Request()
    {
        lock (_gate)
        {
            if (_disposed || _pending)
                return;

            _pending = true;
            _timer?.Dispose();
            _timer = _timeProvider.CreateTimer(_ => OnElapsed(), null, _delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>Bekleyen isteği iptal eder (ör. yenilemeyi zaten elle yaptıysak).</summary>
    public void Cancel()
    {
        lock (_gate)
        {
            _pending = false;
            _timer?.Dispose();
            _timer = null;
        }
    }

    private void OnElapsed()
    {
        lock (_gate)
        {
            if (_disposed || !_pending)
                return;

            _pending = false;
        }

        if (_post is null)
            _action();
        else
            _post(() =>
            {
                lock (_gate)
                {
                    if (_disposed)
                        return;
                }
                _action();
            });
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _pending = false;
            _timer?.Dispose();
            _timer = null;
        }
    }
}

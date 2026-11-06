namespace Koy.Core.Display;

/// <summary>
/// Parlaklık yazımlarını birleştirir: kaydırıcı saniyede onlarca değer üretir, ama DDC/CI çağrısı yüzlerce ms
/// sürebilir. Bekleyen değer tek yer tutar (yenisi eskisini ezer); yalnız bir çalışan işçi olur.
/// Çağıran <see cref="Post"/> true dönerse işçiyi başlatır, işçi <see cref="TryTake"/> false dönene kadar döner.
/// </summary>
public sealed class BrightnessWriteQueue
{
    private readonly object _gate = new();
    private int? _pending;
    private bool _running;

    /// <summary>Değeri bekleyen yere yazar (0-100'e kıstırılır). İşçi çalışmıyorsa true: çağıran başlatmalı.</summary>
    public bool Post(int value)
    {
        lock (_gate)
        {
            _pending = Math.Clamp(value, 0, 100);
            if (_running)
                return false;

            _running = true;
            return true;
        }
    }

    /// <summary>Bekleyen son değeri alır. Yoksa işçiyi "durdu" işaretler ve false döner.</summary>
    public bool TryTake(out int value)
    {
        lock (_gate)
        {
            if (_pending is { } pending)
            {
                _pending = null;
                value = pending;
                return true;
            }

            _running = false;
            value = 0;
            return false;
        }
    }
}

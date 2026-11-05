namespace Koy.Core.Tray;

/// <summary>
/// Paylaşılan tepsi penceresinin kullanıcı sayacı (saf mantık). Bırakma sayıdan fazla olursa eksiye düşmez.
/// </summary>
public sealed class TrayHostLease
{
    public int Users { get; private set; }

    public void Acquire() => Users++;

    /// <summary>Bir kullanıcı bırakır; sayaç sıfıra indiyse (pencere kapatılmalı) true.</summary>
    public bool Release()
    {
        if (Users == 0)
            return false;

        return --Users == 0;
    }

    public void Reset() => Users = 0;
}

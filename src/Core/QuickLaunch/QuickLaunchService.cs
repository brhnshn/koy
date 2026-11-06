using Koy.Core.Shell;

namespace Koy.Core.QuickLaunch;

/// <summary>
/// Hızlı başlat listesi: kullanıcının eklediği uygulama / dosya / klasör yolları. Sıra korunur, en çok
/// <see cref="MaxItems"/>, aynı yol (büyük/küçük harf duyarsız) bir kez. Kalıcılık AppSettings.QuickLaunchItems'tır:
/// servis her değişiklikte kaydetme geri çağrısını çağırır (App ayarları yazar). Simge: Platform'daki
/// IconHelper.GetIconBytes(path). UI iş parçacığından kullanılır; <see cref="Changed"/> değişikliği yapan iş
/// parçacığında tetiklenir.
/// </summary>
public interface IQuickLaunchService
{
    const int MaxItems = 8;

    IReadOnlyList<string> Items { get; }

    event EventHandler? Changed;

    /// <summary>Yol eklenir (tırnaklar ve boşluklar temizlenir). Boş, bulunamayan, tekrar ya da liste doluysa false.</summary>
    bool Add(string path);

    bool Remove(string path);

    /// <summary>Öğeyi <paramref name="fromIndex"/>'ten <paramref name="toIndex"/>'e taşır. Geçersiz indeks için false.</summary>
    bool Move(int fromIndex, int toIndex);

    /// <summary>Kabuk servisi ile başlatır. Başlatılamazsa (silinmiş yol, izin) false; çökme yok.</summary>
    bool Launch(string path);
}

public sealed class QuickLaunchService : IQuickLaunchService
{
    private readonly List<string> _items;
    private readonly Action<IReadOnlyList<string>> _save;
    private readonly IShellActionService _shell;
    private readonly Func<string, bool> _exists;

    public QuickLaunchService(
        IEnumerable<string>? initial,
        Action<IReadOnlyList<string>> save,
        IShellActionService shell,
        Func<string, bool>? exists = null)
    {
        _save = save;
        _shell = shell;
        _exists = exists ?? (p => File.Exists(p) || Directory.Exists(p));

        // Elle bozulmuş ayar dosyasına karşı: temizle, tekrarları at, sınırı uygula (var olmayanlar kalır, kullanıcı kaldırır).
        _items = [];
        foreach (var raw in initial ?? [])
        {
            var path = Normalize(raw);
            if (path is not null && !Contains(path) && _items.Count < IQuickLaunchService.MaxItems)
                _items.Add(path);
        }
    }

    public IReadOnlyList<string> Items => _items;

    public event EventHandler? Changed;

    public bool Add(string path)
    {
        var normalized = Normalize(path);
        if (normalized is null || _items.Count >= IQuickLaunchService.MaxItems || Contains(normalized) || !_exists(normalized))
            return false;

        _items.Add(normalized);
        Commit();
        return true;
    }

    public bool Remove(string path)
    {
        var normalized = Normalize(path);
        var index = normalized is null ? -1 : _items.FindIndex(p => Same(p, normalized));
        if (index < 0)
            return false;

        _items.RemoveAt(index);
        Commit();
        return true;
    }

    public bool Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _items.Count || toIndex < 0 || toIndex >= _items.Count)
            return false;
        if (fromIndex == toIndex)
            return true;

        var item = _items[fromIndex];
        _items.RemoveAt(fromIndex);
        _items.Insert(toIndex, item);
        Commit();
        return true;
    }

    public bool Launch(string path)
    {
        try
        {
            _shell.Launch(path);
            return true;
        }
        catch (Exception ex)
        {
            Diagnostics.CrashLog.Write("QuickLaunchService.Launch", ex);
            return false;
        }
    }

    /// <summary>Listede gösterilecek ad: dosyada uzantısız ad, klasörde klasör adı.</summary>
    public static string DisplayName(string path)
    {
        var trimmed = path.TrimEnd('\\', '/');
        var name = Path.GetFileName(trimmed);
        if (string.IsNullOrEmpty(name))
            return trimmed;
        var withoutExt = Path.GetFileNameWithoutExtension(name);
        return withoutExt.Length > 0 ? withoutExt : name;
    }

    private static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        var p = path.Trim().Trim('"').Trim();
        return p.Length == 0 ? null : p;
    }

    private static bool Same(string a, string b) =>
        string.Equals(a.TrimEnd('\\', '/'), b.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

    private bool Contains(string path) => _items.Any(p => Same(p, path));

    private void Commit()
    {
        _save(_items.ToList());
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

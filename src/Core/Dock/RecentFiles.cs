namespace Koy.Core.Dock;

/// <summary>Son kullanılanlar klasöründeki ham kayıt (kısayol dosyası yolu ve son yazılma zamanı).</summary>
public sealed record RecentEntry(string ShortcutPath, DateTime LastWriteUtc);

/// <summary>Dock'ta gösterilen son kullanılan dosya: kullanıcıya görünen ad ve açmak için verilecek yol.</summary>
public sealed record RecentFile(string Name, string OpenPath, DateTime LastUsedUtc);

/// <summary>
/// Son kullanılan dosyaları okuma sözleşmesi. Yalnız dock'taki yığın açılınca çağrılır; sürekli izleme yoktur.
/// </summary>
public interface IRecentFilesService
{
    IReadOnlyList<RecentFile> GetRecent(int max = RecentFilesPolicy.MaxItems);
}

/// <summary>Son kullanılanlar listesini süzer ve sıralar (saf mantık).</summary>
public static class RecentFilesPolicy
{
    /// <summary>Dock'ta gösterilen en çok öğe sayısı.</summary>
    public const int MaxItems = 10;

    /// <summary>
    /// Ham kayıtlardan görünecek listeyi üretir: yeniden eskiye sıralı, .lnk olmayan ve gizli/sistem dosyaları
    /// (desktop.ini) atılmış, aynı adlılar teke indirilmiş, en çok <paramref name="max"/> öğe (üst sınır
    /// <see cref="MaxItems"/>). Ad, ".lnk" uzantısı atılmış dosya adıdır; açmak için kısayolun kendisi verilir
    /// (Windows hedefi açar).
    /// </summary>
    public static IReadOnlyList<RecentFile> Select(IEnumerable<RecentEntry> entries, int max = MaxItems)
    {
        var limit = Math.Clamp(max, 0, MaxItems);
        if (limit == 0)
            return [];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<RecentFile>();

        foreach (var entry in entries.OrderByDescending(e => e.LastWriteUtc))
        {
            if (string.IsNullOrWhiteSpace(entry.ShortcutPath))
                continue;

            var fileName = Path.GetFileName(entry.ShortcutPath);
            if (!fileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                continue;

            var name = fileName[..^4];
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                continue;

            result.Add(new RecentFile(name, entry.ShortcutPath, entry.LastWriteUtc));
            if (result.Count == limit)
                break;
        }

        return result;
    }
}

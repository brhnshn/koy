using System.IO;
using Koy.Core.Dock;

namespace Koy.Platform.Shell;

/// <summary>
/// Windows'un Son Kullanılanlar klasörünü (%AppData%\Microsoft\Windows\Recent) okur. Her çağrıda klasör bir kez
/// listelenir; izleyici, önbellek ya da arka plan işi yoktur (yalnız dock'taki yığın açılınca çağrılır).
/// Okuma hatası (klasör yok, erişim yok) boş liste verir, istisna fırlatmaz.
/// </summary>
public sealed class RecentFilesService : IRecentFilesService
{
    private static readonly string RecentDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"Microsoft\Windows\Recent");

    public IReadOnlyList<RecentFile> GetRecent(int max = RecentFilesPolicy.MaxItems)
    {
        try
        {
            if (!Directory.Exists(RecentDir))
                return [];

            var entries = new DirectoryInfo(RecentDir)
                .EnumerateFiles("*.lnk", SearchOption.TopDirectoryOnly)
                .Select(f => new RecentEntry(f.FullName, f.LastWriteTimeUtc));

            return RecentFilesPolicy.Select(entries, max);
        }
        catch
        {
            return [];
        }
    }
}

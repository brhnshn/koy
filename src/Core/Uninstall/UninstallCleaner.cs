namespace Koy.Core.Uninstall;

/// <summary>Silme sonucu: kaç dosya silindi, hangileri silinemedi.</summary>
public sealed record UninstallCleanResult(int Deleted, IReadOnlyList<string> Failed);

/// <summary>
/// <see cref="UninstallPlan"/>'ın listesini siler. Hiçbir koşulda istisna fırlatmaz: kaldırma akışı
/// kilitli bir dosya yüzünden durmamalı.
/// </summary>
public static class UninstallCleaner
{
    public static UninstallCleanResult Run(string roamingDir, string localDir, bool keepSettings)
    {
        var deleted = 0;
        var failed = new List<string>();

        foreach (var file in UninstallPlan.FilesToDelete(roamingDir, localDir, keepSettings))
        {
            try
            {
                if (!File.Exists(file))
                    continue;
                File.Delete(file);
                deleted++;
            }
            catch
            {
                failed.Add(file);
            }
        }

        // Ayarlar silindiyse boşalan %AppData%\Koy klasörü de gider. Yalnız boşsa: Directory.Delete özyinelemesiz,
        // başka bir şey kaldıysa (kullanıcının dosyası) dokunmaz.
        if (!keepSettings)
            TryRemoveEmptyDirectory(roamingDir);

        return new UninstallCleanResult(deleted, failed);
    }

    private static void TryRemoveEmptyDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir, recursive: false);
        }
        catch
        {
            // Boş klasör kalması zararsız.
        }
    }
}

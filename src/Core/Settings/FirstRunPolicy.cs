namespace Koy.Core.Settings;

/// <summary>
/// İlk kurulum sihirbazı tamamlanmadan (ör. X ile kapatılınca) uygulamanın kullanıcının görev çubuğuna dokunmaması için
/// güvenli varsayılanlar. Yalnız bellekteki ayara uygulanır, diske yazılmaz: sihirbaz bir sonraki açılışta yeniden çıkar.
/// </summary>
public static class FirstRunPolicy
{
    public static AppSettings ApplySafeDefaultsIfIncomplete(AppSettings settings)
    {
        if (settings.FirstRunCompleted)
            return settings;

        // Dock görev çubuğunu gizler (AGENTS.md #12); kullanıcı henüz seçim yapmadığı için kapalı kalır.
        return settings with { DockEnabled = false, HideWindowsTaskbar = false };
    }

    /// <summary>
    /// "Ayarları sıfırla" için varsayılan ayarlar. Kurulum sihirbazı zaten tamamlandığından
    /// <see cref="AppSettings.FirstRunCompleted"/> korunur; yoksa sıfırlama sihirbazı yeniden açardı.
    /// </summary>
    public static AppSettings CreateReset(AppSettings current) =>
        new() { FirstRunCompleted = current.FirstRunCompleted };
}

using Koy.Core.Display;

namespace Koy.Core.Settings;

/// <summary>Eski sürümlerin ayar dosyalarını güncel şemaya taşır. Saf: monitör listesi gerekmez.</summary>
public static class SettingsMigration
{
    /// <summary>
    /// Sürüm 2 → 3: tek hedefli <c>MonitorTarget</c> yerine monitör başına ayar.
    /// <list type="bullet">
    /// <item>"Primary" ve "All": eski varsayılan "yalnız ana ekran" idi ve değiştirmek için arayüz yoktu; çoklu ekran artık
    /// gerçek olduğundan kayıt yazılmaz, bütün ekranlar yeni ekran varsayılanını (hepsi açık) alır.</item>
    /// <item>Belirli ekran (elle yazılmış aygıt adı ya da kimlik): yalnız o ekran açık kalır, diğerleri (yeni ekran
    /// varsayılanı "hiçbiri" olduğundan) kapalı. Anahtar eski haliyle yazılır; ekran ilk bulunduğunda
    /// <see cref="MonitorPolicy.KeyMatches"/> eşler, ilk düzenlemede kararlı kimliğe çevrilir.</item>
    /// </list>
    /// </summary>
    public static AppSettings Migrate(AppSettings settings)
    {
        if (settings.Version >= AppSettings.CurrentVersion)
            return settings;

        var monitors = settings.Monitors is null
            ? new Dictionary<string, MonitorSettings>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, MonitorSettings>(settings.Monitors, StringComparer.OrdinalIgnoreCase);
        var newScreenDefault = settings.NewScreenDefault;

        if (DisplayLayoutResolver.ParseMode(settings.MonitorTarget) == MonitorTargetMode.Specific &&
            !string.IsNullOrWhiteSpace(settings.MonitorTarget))
        {
            monitors[settings.MonitorTarget] = new MonitorSettings { NotchEnabled = true, DockEnabled = true };
            newScreenDefault = NewScreenMode.None;
        }

        return settings with
        {
            Version = AppSettings.CurrentVersion,
            Monitors = monitors,
            NewScreenDefault = newScreenDefault
        };
    }
}

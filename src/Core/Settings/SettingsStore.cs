using System.Text.Json;

namespace Koy.Core.Settings;

public sealed class SettingsStore(string path)
{
    public static string DefaultPath { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Koy",
        "settings.json");

    public string Path { get; } = path;

    /// <summary>Dosya yoksa ya da JSON'u bozuksa varsayılan ayarları döner. Okuma sırasındaki G/Ç hataları (kilitli dosya vb.) yakalanmaz.</summary>
    public AppSettings Load()
    {
        AppSettings? settings = null;
        try
        {
            if (!File.Exists(Path))
                return new AppSettings();

            using (var stream = File.OpenRead(Path))
            {
                settings = JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.AppSettings);
            }
        }
        catch (JsonException)
        {
            return new AppSettings();
        }

        return settings is null ? new AppSettings() : SettingsMigration.Migrate(settings);
    }

    /// <summary>Önce geçici dosyaya yazar, sonra yerine taşır; yazma yarıda kesilirse eski dosya bozulmaz.</summary>
    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var temp = Path + ".tmp";
        using (var stream = File.Create(temp))
            JsonSerializer.Serialize(stream, settings, SettingsJsonContext.Default.AppSettings);
        File.Move(temp, Path, overwrite: true);
    }
}

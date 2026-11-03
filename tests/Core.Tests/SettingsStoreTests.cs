using Koy.Core.Settings;
using Koy.Core.Visibility;

namespace Koy.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "koy-tests-" + Guid.NewGuid());

    private SettingsStore NewStore() => new(Path.Combine(_dir, "settings.json"));

    [Fact]
    public void Missing_file_returns_defaults()
    {
        Assert.Equal(new AppSettings(), NewStore().Load());
    }

    [Fact]
    public void Round_trips()
    {
        var store = NewStore();
        var settings = new AppSettings { Language = "en", DockVisibility = VisibilityMode.Peek, AccentColor = "#FF8800" };

        store.Save(settings);

        Assert.Equal(settings, store.Load());
    }

    [Fact]
    public void Corrupt_file_returns_defaults()
    {
        var store = NewStore();
        Directory.CreateDirectory(_dir);
        File.WriteAllText(store.Path, "{ bozuk");

        Assert.Equal(new AppSettings(), store.Load());
    }

    [Fact]
    public void Enums_are_written_as_text()
    {
        var store = NewStore();
        store.Save(new AppSettings());

        Assert.Contains("\"Smart\"", File.ReadAllText(store.Path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}

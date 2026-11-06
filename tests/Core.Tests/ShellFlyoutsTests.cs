using Koy.Core.Shell;
using Xunit;

namespace Koy.Core.Tests;

public class ShellFlyoutsTests
{
    [Theory]
    [InlineData("StartMenuExperienceHost")]
    [InlineData("startmenuexperiencehost")]
    [InlineData("SearchHost")]
    [InlineData("SearchApp")]
    [InlineData("ShellExperienceHost")]
    public void Kabuk_acilir_pencere_surecleri_taninir(string name) =>
        Assert.True(ShellFlyouts.IsFlyoutProcess(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("explorer")]
    [InlineData("chrome")]
    [InlineData("StartMenuExperienceHost.exe")]
    public void Siradan_surecler_kabuk_acilir_penceresi_sayilmaz(string? name) =>
        Assert.False(ShellFlyouts.IsFlyoutProcess(name));
}

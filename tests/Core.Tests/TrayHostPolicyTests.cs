using Koy.Core.Tray;
using Xunit;

namespace Koy.Core.Tests;

public class TrayHostPolicyTests
{
    [Theory]
    [InlineData(true, true, 1, true)]
    [InlineData(true, true, 3, true)]
    [InlineData(true, true, 0, false)]   // dock penceresi yok: görev çubuğu geri gelir, tepsiye dokunulmaz
    [InlineData(false, true, 2, false)]  // görev çubuğu görünür: Windows'un tepsisine dokunulmaz
    [InlineData(true, false, 2, false)]  // ayar kapalı
    [InlineData(false, false, 0, false)]
    public void ShouldHost_OnlyWhenTaskbarIsActuallyHidden(bool hide, bool show, int windows, bool expected) =>
        Assert.Equal(expected, TrayHostPolicy.ShouldHost(hide, show, windows));

    [Fact]
    public void IsOwnIcon_MatchesOnlyKoyProcess()
    {
        Assert.True(TrayHostPolicy.IsOwnIcon(4242, 4242));
        Assert.False(TrayHostPolicy.IsOwnIcon(1111, 4242));
    }

    [Fact]
    public void IsOwnIcon_UnknownOwnerIsNotFiltered()
    {
        // GetWindowThreadProcessId geçersiz pencerede 0 döner; sahte "kendi simgesi" sayılmaz (ölü pencere temizliği halleder).
        Assert.False(TrayHostPolicy.IsOwnIcon(0, 0));
        Assert.False(TrayHostPolicy.IsOwnIcon(0, 4242));
    }
}

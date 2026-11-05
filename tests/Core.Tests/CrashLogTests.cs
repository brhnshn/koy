using Koy.Core.Diagnostics;

namespace Koy.Core.Tests;

public class CrashLogTests
{
    [Fact]
    public void Format_ZamanTipMesajVeYiginiIcerir()
    {
        Exception ex;
        try { throw new InvalidOperationException("deneme mesaji"); }
        catch (Exception e) { ex = e; }

        var text = CrashLog.Format("Dispatcher", ex, new DateTime(2026, 11, 5, 17, 11, 38));

        Assert.Contains("[2026-11-05 17:11:38] Dispatcher", text);
        Assert.Contains("System.InvalidOperationException", text);
        Assert.Contains("deneme mesaji", text);
        Assert.Contains("Format_ZamanTipMesajVeYiginiIcerir", text);
    }

    [Fact]
    public void Format_IstisnaOlmayanNesneyiYazar()
    {
        var text = CrashLog.Format("AppDomain", "garip nesne", DateTime.Now);
        Assert.Contains("garip nesne", text);
    }

    [Fact]
    public void WriteTo_OlmayanKlasoruOlusturupEkler()
    {
        var dir = Path.Combine(Path.GetTempPath(), "koy-crashlog-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "alt", "crash.log");
        try
        {
            CrashLog.WriteTo(path, "A", new Exception("bir"));
            CrashLog.WriteTo(path, "B", new Exception("iki"));

            var content = File.ReadAllText(path);
            Assert.Contains("bir", content);
            Assert.Contains("iki", content);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void WriteTo_GecersizYolIstisnaFirlatmaz()
    {
        CrashLog.WriteTo("Z:\\olmayan\\\0\\crash.log", "X", new Exception("x"));
    }

    [Fact]
    public void Guard_IstisnayiYutarVeLogaYazar()
    {
        var dir = Path.Combine(Path.GetTempPath(), "koy-crashlog-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "crash.log");
        var old = CrashLog.LogPath;
        try
        {
            CrashLog.LogPath = path;
            var ran = false;
            CrashLog.Guard("test", () => { ran = true; throw new Exception("sizmamali"); });

            Assert.True(ran);
            Assert.Contains("sizmamali", File.ReadAllText(path));
        }
        finally
        {
            CrashLog.LogPath = old;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}

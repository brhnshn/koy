using Koy.Core.Clipboard;

namespace Koy.Core.Tests;

public sealed class ClipboardDisplayTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 14, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, AgeUnit.Now, 0)]
    [InlineData(59, AgeUnit.Now, 0)]
    [InlineData(60, AgeUnit.Minutes, 1)]
    [InlineData(2 * 60 + 10, AgeUnit.Minutes, 2)]
    [InlineData(59 * 60, AgeUnit.Minutes, 59)]
    [InlineData(60 * 60, AgeUnit.Hours, 1)]
    [InlineData(5 * 3600 + 100, AgeUnit.Hours, 5)]
    [InlineData(24 * 3600, AgeUnit.Days, 1)]
    [InlineData(3 * 86400 + 5, AgeUnit.Days, 3)]
    public void Age_PicksTheLargestFittingUnit(int seconds, AgeUnit unit, int value)
    {
        Assert.Equal((unit, value), ClipboardDisplay.Age(Now.AddSeconds(-seconds), Now));
    }

    [Fact]
    public void Age_FutureTimestamp_IsNow()
    {
        // Saat geri alınırsa negatif süre "şimdi" sayılır.
        Assert.Equal((AgeUnit.Now, 0), ClipboardDisplay.Age(Now.AddMinutes(5), Now));
    }

    [Fact]
    public void Preview_TakesFirstLinesAndSkipsBlanks()
    {
        var text = "  ilk\n\n   ikinci  \r\nüçüncü\ndördüncü";
        Assert.Equal("ilk\nikinci\nüçüncü", ClipboardDisplay.Preview(text));
        Assert.Equal("ilk", ClipboardDisplay.Preview(text, maxLines: 1));
    }

    [Fact]
    public void Preview_CutsLongLines()
    {
        var result = ClipboardDisplay.Preview(new string('a', 200), maxLineChars: 10);
        Assert.Equal(new string('a', 10) + "…", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n ")]
    public void Preview_EmptyInput_IsEmpty(string? text)
    {
        Assert.Equal("", ClipboardDisplay.Preview(text));
    }

    [Theory]
    [InlineData("#3B82F6", 0x3B, 0x82, 0xF6, 255)]
    [InlineData("#fff", 255, 255, 255, 255)]
    [InlineData("#0af", 0x00, 0xAA, 0xFF, 255)]
    [InlineData("#11223380", 0x11, 0x22, 0x33, 0x80)]
    [InlineData("rgb(59, 130, 246)", 59, 130, 246, 255)]
    [InlineData("RGB(0,0,0)", 0, 0, 0, 255)]
    [InlineData("rgba(10, 20, 30, 0.5)", 10, 20, 30, 128)]
    [InlineData("rgba(10, 20, 30, 50%)", 10, 20, 30, 128)]
    [InlineData("rgba(10, 20, 30, 1)", 10, 20, 30, 255)]
    public void TryParseColor_ReadsKnownForms(string text, int r, int g, int b, int a)
    {
        Assert.True(ClipboardDisplay.TryParseColor(text, out var rr, out var gg, out var bb, out var aa));
        Assert.Equal((r, g, b, a), ((int)rr, (int)gg, (int)bb, (int)aa));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    [InlineData("rgb(300, 0, 0)")]
    [InlineData("rgb(1, 2)")]
    [InlineData("mavi")]
    public void TryParseColor_RejectsOthers(string? text)
    {
        Assert.False(ClipboardDisplay.TryParseColor(text, out _, out _, out _, out _));
    }

    [Fact]
    public void Classifier_ColorsAlwaysParse()
    {
        // Sınıflandırıcının "Renk" dediği her metin gösterimde de çözülebilmeli.
        foreach (var text in new[] { "#abc", "#aabbcc", "#aabbccdd", "rgb(1,2,3)", "rgba(1, 2, 3, 0.4)", "rgba(1,2,3,40%)" })
        {
            Assert.Equal(ClipboardKind.Color, ClipboardClassifier.Classify(text));
            Assert.True(ClipboardDisplay.TryParseColor(text, out _, out _, out _, out _), text);
        }
    }
}

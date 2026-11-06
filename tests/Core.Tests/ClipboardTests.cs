using Koy.Core.Clipboard;

namespace Koy.Core.Tests;

public class ClipboardClassifierTests
{
    [Theory]
    [InlineData("https://koy.app/path?q=1", ClipboardKind.Link)]
    [InlineData("  http://example.com  ", ClipboardKind.Link)]
    [InlineData("ftp://example.com", ClipboardKind.Text)]
    [InlineData("https://a.com ve baska metin", ClipboardKind.Text)]
    [InlineData("example.com", ClipboardKind.Text)]
    [InlineData("#FFF", ClipboardKind.Color)]
    [InlineData("#a78bfa", ClipboardKind.Color)]
    [InlineData("#A78BFA80", ClipboardKind.Color)]
    [InlineData("#12345", ClipboardKind.Text)]
    [InlineData("#GGGGGG", ClipboardKind.Text)]
    [InlineData("rgb(10, 20, 30)", ClipboardKind.Color)]
    [InlineData("rgba(10,20,30,0.5)", ClipboardKind.Color)]
    [InlineData("RGB(255,255,255)", ClipboardKind.Color)]
    [InlineData("rgb(10, 20)", ClipboardKind.Text)]
    [InlineData("Merhaba dünya", ClipboardKind.Text)]
    [InlineData("", ClipboardKind.Text)]
    [InlineData("   ", ClipboardKind.Text)]
    public void Classifies_simple_kinds(string text, ClipboardKind expected) =>
        Assert.Equal(expected, ClipboardClassifier.Classify(text));

    [Fact]
    public void Null_is_text() => Assert.Equal(ClipboardKind.Text, ClipboardClassifier.Classify(null));

    [Fact]
    public void Multiline_code_with_braces_and_semicolons_is_code()
    {
        const string code = "public int Add(int a, int b)\n{\n    return a + b;\n}";
        Assert.Equal(ClipboardKind.Code, ClipboardClassifier.Classify(code));
    }

    [Fact]
    public void Python_with_indentation_and_keyword_is_code()
    {
        const string code = "def topla(a, b):\n    sonuc = a + b\n    return sonuc";
        Assert.Equal(ClipboardKind.Code, ClipboardClassifier.Classify(code));
    }

    [Fact]
    public void Html_block_is_code()
    {
        Assert.Equal(ClipboardKind.Code, ClipboardClassifier.Classify("<div class=\"a\">\n  <span>x</span>\n</div>"));
    }

    [Theory]
    [InlineData("var x = Foo(1);")]
    [InlineData("const f = (a) => a + 1")]
    [InlineData("if (x == 1) {")]
    public void Single_line_with_strong_signal_is_code(string line) =>
        Assert.Equal(ClipboardKind.Code, ClipboardClassifier.Classify(line));

    [Fact]
    public void Plain_multiline_prose_is_text()
    {
        const string prose = "Merhaba,\nYarınki toplantı saat 10'da.\nGörüşürüz.";
        Assert.Equal(ClipboardKind.Text, ClipboardClassifier.Classify(prose));
    }

    [Fact]
    public void Single_line_prose_with_a_semicolon_or_parenthesis_is_text()
    {
        Assert.Equal(ClipboardKind.Text, ClipboardClassifier.Classify("Geldim, gördüm; yendim (neredeyse)"));
        Assert.Equal(ClipboardKind.Text, ClipboardClassifier.Classify("Bunu sonra konuşuruz (belki)."));
    }
}

public class ClipboardHistoryTests
{
    private static DateTimeOffset _t = new(2026, 11, 6, 10, 0, 0, TimeSpan.Zero);

    private static ClipboardHistory NewHistory() => new(() => _t = _t.AddSeconds(1));

    [Fact]
    public void Add_puts_newest_first_and_classifies()
    {
        var h = NewHistory();
        h.Add("bir");
        h.Add("https://a.com");

        var all = h.Query();
        Assert.Equal(["https://a.com", "bir"], all.Select(i => i.Text));
        Assert.Equal(ClipboardKind.Link, all[0].Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n ")]
    public void Blank_text_is_ignored(string? text)
    {
        var h = NewHistory();
        Assert.Null(h.Add(text));
        Assert.Empty(h.Query());
    }

    [Fact]
    public void Duplicate_moves_to_front_without_adding()
    {
        var h = NewHistory();
        var first = h.Add("a")!;
        h.Add("b");
        var again = h.Add("a")!;

        Assert.Equal(first.Id, again.Id);
        Assert.Equal(["a", "b"], h.Query().Select(i => i.Text));
    }

    [Fact]
    public void Long_text_is_truncated()
    {
        var h = NewHistory();
        var item = h.Add(new string('x', ClipboardHistory.MaxTextLength + 500))!;
        Assert.Equal(ClipboardHistory.MaxTextLength, item.Text.Length);
    }

    [Fact]
    public void Keeps_at_most_50_unpinned_dropping_oldest()
    {
        var h = NewHistory();
        for (var i = 0; i < 60; i++)
            h.Add("öğe " + i);

        var all = h.Query();
        Assert.Equal(ClipboardHistory.MaxUnpinned, all.Count);
        Assert.Equal("öğe 59", all[0].Text);
        Assert.DoesNotContain(all, x => x.Text == "öğe 0");
    }

    [Fact]
    public void Pinned_items_do_not_count_toward_the_limit_and_survive_trimming()
    {
        var h = NewHistory();
        var pinned = h.Add("sabit")!;
        h.SetPinned(pinned.Id, true);
        for (var i = 0; i < 70; i++)
            h.Add("öğe " + i);

        Assert.Equal(ClipboardHistory.MaxUnpinned + 1, h.Query().Count);
        Assert.Contains(h.Query(ClipboardFilter.Pinned), x => x.Text == "sabit");
    }

    [Fact]
    public void Duplicate_of_pinned_keeps_it_pinned()
    {
        var h = NewHistory();
        var item = h.Add("a")!;
        h.SetPinned(item.Id, true);
        var again = h.Add("a")!;
        Assert.True(again.Pinned);
        Assert.Single(h.Query());
    }

    [Fact]
    public void Search_is_case_insensitive()
    {
        var h = NewHistory();
        h.Add("Merhaba Dünya");
        h.Add("başka");

        Assert.Single(h.Query(search: "merhaba"));
        Assert.Single(h.Query(search: "DÜNYA"));
        Assert.Empty(h.Query(search: "yok"));
    }

    [Fact]
    public void Filters_select_by_kind_and_pinned()
    {
        var h = NewHistory();
        h.Add("https://a.com");
        h.Add("#FF0000");
        h.Add("düz");
        var code = h.Add("var x = Foo(1);")!;
        h.SetPinned(code.Id, true);

        Assert.Single(h.Query(ClipboardFilter.Link));
        Assert.Single(h.Query(ClipboardFilter.Color));
        Assert.Single(h.Query(ClipboardFilter.Code));
        Assert.Equal(code.Id, Assert.Single(h.Query(ClipboardFilter.Pinned)).Id);
        Assert.Equal(4, h.Query(ClipboardFilter.All).Count);
    }

    [Fact]
    public void Unpinning_over_the_limit_trims_oldest()
    {
        var h = NewHistory();
        var old = h.Add("eski")!;
        h.SetPinned(old.Id, true);
        for (var i = 0; i < ClipboardHistory.MaxUnpinned; i++)
            h.Add("öğe " + i);

        h.SetPinned(old.Id, false);

        Assert.Equal(ClipboardHistory.MaxUnpinned, h.Query().Count);
        Assert.DoesNotContain(h.Query(), x => x.Text == "eski");
    }

    [Fact]
    public void Remove_deletes_single_item_even_if_pinned()
    {
        var h = NewHistory();
        var a = h.Add("a")!;
        h.Add("b");
        h.SetPinned(a.Id, true);

        Assert.True(h.Remove(a.Id));
        Assert.False(h.Remove(a.Id));
        Assert.Equal(["b"], h.Query().Select(i => i.Text));
    }

    [Fact]
    public void Clear_keeps_pinned()
    {
        var h = NewHistory();
        var a = h.Add("a")!;
        h.Add("b");
        h.Add("c");
        h.SetPinned(a.Id, true);

        Assert.Equal(2, h.Clear());
        Assert.Equal(["a"], h.Query().Select(i => i.Text));
    }

    [Fact]
    public void LoadPinned_restores_marked_pinned_and_skips_duplicates()
    {
        var h = NewHistory();
        h.Add("canlı");
        var id = Guid.NewGuid();
        h.LoadPinned([new ClipboardItem(id, "diskten", ClipboardKind.Text, false, _t), new ClipboardItem(Guid.NewGuid(), "canlı", ClipboardKind.Text, true, _t)]);

        Assert.Equal(2, h.Query().Count);
        Assert.True(h.Get(id)!.Pinned);
    }
}

public sealed class ClipboardPinnedStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "koy-clip-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Fact]
    public void Round_trips_only_pinned()
    {
        var store = new ClipboardPinnedStore(Path.Combine(_dir, "p.json"));
        var pinned = new ClipboardItem(Guid.NewGuid(), "sabit", ClipboardKind.Code, true, DateTimeOffset.UnixEpoch);
        var loose = new ClipboardItem(Guid.NewGuid(), "geçici", ClipboardKind.Text, false, DateTimeOffset.UnixEpoch);

        Assert.True(store.Save([pinned, loose]));

        Assert.Equal([pinned], store.Load());
    }

    [Fact]
    public void Missing_file_is_empty_without_error()
    {
        var errors = new List<string>();
        var store = new ClipboardPinnedStore(Path.Combine(_dir, "yok.json"), (s, _) => errors.Add(s));

        Assert.Empty(store.Load());
        Assert.Empty(errors);
    }

    [Fact]
    public void Corrupt_file_is_empty_and_reported()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "p.json");
        File.WriteAllText(path, "{ bozuk");
        var errors = new List<string>();
        var store = new ClipboardPinnedStore(path, (s, _) => errors.Add(s));

        Assert.Empty(store.Load());
        Assert.Single(errors);
    }
}

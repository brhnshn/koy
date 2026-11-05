namespace Koy.Core.Display;

/// <summary>Ekranlar sayfasındaki şemada tek bir monitörün dikdörtgeni (şema koordinatları, DIP).</summary>
public sealed record SchemaTile(
    string Id,
    int Number,
    bool IsPrimary,
    double Left,
    double Top,
    double Width,
    double Height);

/// <summary>Şemanın toplam boyutu ve içindeki dikdörtgenler.</summary>
public sealed record SchemaLayout(double Width, double Height, IReadOnlyList<SchemaTile> Tiles)
{
    public static SchemaLayout Empty { get; } = new(0, 0, []);
}

/// <summary>
/// Gerçek monitör yerleşimini (sanal masaüstü koordinatları) orantılı küçültüp şemaya sığdıran saf hesap.
/// Ekran numarası: ana ekran 1, diğerleri soldan sağa, sonra yukarıdan aşağı. Windows'un kendi numarası
/// sürücü sırasına bağlı olduğundan ayrı, tahmin edilebilir bir sıra kullanılır; "Ekranları tanımla" da aynı numarayı gösterir.
/// </summary>
public static class DisplaySchema
{
    /// <summary>Ekranları numara sırasına dizer (ana ekran, sonra soldan sağa, yukarıdan aşağı).</summary>
    public static IReadOnlyList<DisplayMonitor> Order(IReadOnlyList<DisplayMonitor> monitors) =>
        monitors
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.Bounds.Left)
            .ThenBy(m => m.Bounds.Top)
            .ThenBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Monitörün şema/tanımla numarası (1'den başlar); listede yoksa 0.</summary>
    public static int NumberOf(IReadOnlyList<DisplayMonitor> monitors, string id)
    {
        var ordered = Order(monitors);
        for (var i = 0; i < ordered.Count; i++)
            if (string.Equals(ordered[i].Id, id, StringComparison.OrdinalIgnoreCase))
                return i + 1;
        return 0;
    }

    /// <summary>
    /// Ekranları en fazla <paramref name="maxWidth"/> × <paramref name="maxHeight"/> alana tek ölçekle sığdırır
    /// (en-boy oranı korunur, büyütme yapılmaz). Bitişik ekranlar tek blok görünmesin diye her dikdörtgen
    /// <paramref name="gap"/> kadar içeri çekilir.
    /// </summary>
    public static SchemaLayout Compute(IReadOnlyList<DisplayMonitor> monitors, double maxWidth, double maxHeight, double gap = 8)
    {
        var valid = monitors.Where(m => !m.Bounds.IsEmpty).ToList();
        if (valid.Count == 0 || maxWidth <= 0 || maxHeight <= 0)
            return SchemaLayout.Empty;

        var minX = valid.Min(m => m.Bounds.Left);
        var minY = valid.Min(m => m.Bounds.Top);
        var spanX = valid.Max(m => m.Bounds.Right) - minX;
        var spanY = valid.Max(m => m.Bounds.Bottom) - minY;

        // Sanal masaüstü koordinatları fiziksel pikseldir ve ekranlar arasında tutarlıdır; ölçek tek.
        var scale = Math.Min(Math.Min(maxWidth / spanX, maxHeight / spanY), 1.0);

        var ordered = Order(valid);
        var tiles = new List<SchemaTile>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var m = ordered[i];
            var width = m.Bounds.Width * scale;
            var height = m.Bounds.Height * scale;
            var insetX = Math.Min(gap / 2, width / 4);
            var insetY = Math.Min(gap / 2, height / 4);

            tiles.Add(new SchemaTile(
                m.Id,
                i + 1,
                m.IsPrimary,
                (m.Bounds.Left - minX) * scale + insetX,
                (m.Bounds.Top - minY) * scale + insetY,
                width - 2 * insetX,
                height - 2 * insetY));
        }

        return new SchemaLayout(spanX * scale, spanY * scale, tiles);
    }
}

namespace Koy.Core.Display;

/// <summary>
/// Ayarlar'daki monitör haritasında tek bir ekranın kutusu. Konum ve boyut harita koordinatlarındadır (DIP).
/// </summary>
public sealed record MonitorMapTile(
    string Id,
    int Number,
    bool IsPrimary,
    double Left,
    double Top,
    double Width,
    double Height,
    int PixelWidth,
    int PixelHeight,
    bool HasNotch,
    bool HasDock);

/// <summary>
/// Haritanın toplam boyutu ve içindeki ekran kutuları.
/// </summary>
public sealed record MonitorMapLayoutResult(
    double Width,
    double Height,
    IReadOnlyList<MonitorMapTile> Tiles)
{
    public static MonitorMapLayoutResult Empty { get; } = new(0, 0, []);
}

/// <summary>
/// Bağlı ekranları gerçek yerleşimleriyle (yan yana, üst üste) küçültüp haritaya sığdırır.
/// </summary>
public static class MonitorMapLayout
{
    /// <summary>
    /// Ekranları en fazla <paramref name="maxWidth"/> × <paramref name="maxHeight"/> alana orantılı sığdırır.
    /// Kutular arasında <paramref name="gap"/> kadar boşluk bırakılır; ana ekran 1 numaradır.
    /// Çentik ve dock işaretleri <paramref name="monitorTarget"/> ayarına göre belirlenir.
    /// </summary>
    public static MonitorMapLayoutResult Compute(
        IReadOnlyList<DisplayMonitor> monitors,
        double maxWidth,
        double maxHeight,
        string monitorTarget,
        bool notchEnabled,
        bool dockEnabled,
        double gap = 12)
    {
        var valid = monitors
            .Where(m => !m.Bounds.IsEmpty)
            .ToList();

        if (valid.Count == 0 || maxWidth <= 0 || maxHeight <= 0)
            return MonitorMapLayoutResult.Empty;

        // Fiziksel piksellerle çalışılır: sanal masaüstü koordinatları zaten fiziksel ve ekranlar arasında tutarlı.
        var minX = valid.Min(m => m.Bounds.Left);
        var minY = valid.Min(m => m.Bounds.Top);
        var spanX = valid.Max(m => m.Bounds.Right) - minX;
        var spanY = valid.Max(m => m.Bounds.Bottom) - minY;
        var scale = Math.Min(maxWidth / spanX, maxHeight / spanY);

        var targets = DisplayLayoutResolver.ResolveTargetMonitors(valid, monitorTarget);
        var targetIds = new HashSet<string>(targets.Select(t => t.Id), StringComparer.OrdinalIgnoreCase);

        // Ana ekran 1; diğerleri soldan sağa, sonra yukarıdan aşağı. Windows'un ekran numarası
        // sürücü sırasına bağlı ve bize ulaşmıyor; tutarlı ve tahmin edilebilir bir sıra seçildi.
        var ordered = valid
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.Bounds.Left)
            .ThenBy(m => m.Bounds.Top)
            .ToList();

        var inset = gap / 2;
        var tiles = new List<MonitorMapTile>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var m = ordered[i];
            var left = (m.Bounds.Left - minX) * scale;
            var top = (m.Bounds.Top - minY) * scale;
            var width = m.Bounds.Width * scale;
            var height = m.Bounds.Height * scale;

            // Bitişik ekranlar haritada tek blok gibi görünmesin diye her kutu kenarlardan içeri çekilir.
            var insetX = Math.Min(inset, width / 4);
            var insetY = Math.Min(inset, height / 4);

            var isTarget = targetIds.Contains(m.Id);
            tiles.Add(new MonitorMapTile(
                Id: m.Id,
                Number: i + 1,
                IsPrimary: m.IsPrimary,
                Left: left + insetX,
                Top: top + insetY,
                Width: width - 2 * insetX,
                Height: height - 2 * insetY,
                PixelWidth: m.Bounds.Width,
                PixelHeight: m.Bounds.Height,
                HasNotch: notchEnabled && isTarget,
                HasDock: dockEnabled && isTarget));
        }

        return new MonitorMapLayoutResult(spanX * scale, spanY * scale, tiles);
    }
}

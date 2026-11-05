using Koy.Core.Shell;

namespace Koy.Core.Dock;

/// <summary>
/// Pencere önizlemesini kutuya en-boy oranını koruyarak sığdırır ve ortalar.
/// DWM küçük resmi verilen dikdörtgene gerilir; oran korunmazsa geniş pencere ezik görünür.
/// </summary>
public static class ThumbnailFit
{
    /// <summary>
    /// <paramref name="sourceWidth"/> × <paramref name="sourceHeight"/> boyutlu kaynağı <paramref name="box"/> içine sığdırır.
    /// Kaynak kutudan küçükse büyütülmez (bulanıklaşmasın). Kaynak boyutu bilinmiyorsa kutunun kendisi döner.
    /// </summary>
    public static WindowRect Fit(int sourceWidth, int sourceHeight, WindowRect box)
    {
        if (box.IsEmpty)
            return WindowRect.Empty;

        if (sourceWidth <= 0 || sourceHeight <= 0)
            return box;

        var scale = Math.Min(1.0, Math.Min((double)box.Width / sourceWidth, (double)box.Height / sourceHeight));
        var width = Math.Max(1, (int)Math.Round(sourceWidth * scale));
        var height = Math.Max(1, (int)Math.Round(sourceHeight * scale));

        var left = box.Left + (box.Width - width) / 2;
        var top = box.Top + (box.Height - height) / 2;
        return new WindowRect(left, top, left + width, top + height);
    }
}

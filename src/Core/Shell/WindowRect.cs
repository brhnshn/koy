namespace Koy.Core.Shell;

/// <summary>
/// Ekran koordinatlarında pencere sınırları (Win32 fiziksel piksel koordinatları).
/// </summary>
public readonly record struct WindowRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Math.Max(0, Right - Left);
    public int Height => Math.Max(0, Bottom - Top);
    public bool IsEmpty => Width == 0 || Height == 0;

    public bool Intersects(WindowRect other) =>
        Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;

    public bool Contains(int x, int y) =>
        x >= Left && x < Right && y >= Top && y < Bottom;

    public static WindowRect Empty => default;
}

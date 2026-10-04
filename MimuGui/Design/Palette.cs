using Avalonia.Media;

namespace MimuGui.Design;

public static class Palette
{
    public static readonly IBrush Black = Brush.Parse("#000000");
    public static readonly IBrush White = Brush.Parse("#FFFFFF");
    public static readonly IBrush Gray = Brush.Parse("#74757C");
    public static readonly IBrush Purple = Brush.Parse("#6A4C93");
    public static readonly IBrush Blue = Brush.Parse("#16395C");
    public static readonly IBrush Red = Brush.Parse("#B3261E");
    public static readonly IBrush Green = Brush.Parse("#4CAF50");
    public static readonly IBrush Yellow = Brush.Parse("#FDD835");

    public static readonly IBrush Background = Brush.Parse("#08090B");
    public static readonly IBrush Surface = Brush.Parse("#0D0E11");
    public static readonly IBrush SurfaceContainerLowest = Brush.Parse("#060708");
    public static readonly IBrush SurfaceContainerLow = Brush.Parse("#141518");
    public static readonly IBrush SurfaceContainer = Brush.Parse("#17181B");
    public static readonly IBrush SurfaceContainerHigh = Brush.Parse("#202126");
    public static readonly IBrush SurfaceContainerHighest = Brush.Parse("#2A2B30");
    public static readonly IBrush OnSurface = Brush.Parse("#E2E2E6");
    public static readonly IBrush OnSurfaceVariant = Brush.Parse("#A9AAB1");
    public static readonly IBrush Outline = Brush.Parse("#74757C");
    public static readonly IBrush OutlineVariant = Brush.Parse("#26272C");

    public static readonly IBrush Scrim = Black;
    public static readonly IBrush Primary = Purple;
    public static readonly IBrush Accent = Purple;
    public static readonly IBrush Error = Red;
    public static readonly IBrush Online = Green;
}

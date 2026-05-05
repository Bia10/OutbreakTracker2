namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal sealed class MapSectionDetailGeometry(
    MapSectionDetailKind kind,
    int x,
    int y,
    int width,
    int height,
    MapSectionDetailOrientation orientation = MapSectionDetailOrientation.Horizontal
)
{
    public MapSectionDetailKind Kind { get; } = kind;

    public int X { get; } = x;

    public int Y { get; } = y;

    public int Width { get; } = width;

    public int Height { get; } = height;

    public int Right { get; } = x + width - 1;

    public int Bottom { get; } = y + height - 1;

    public MapSectionDetailOrientation Orientation { get; } = orientation;
}

namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal sealed class MapSectionGeometry(
    int width,
    int height,
    IReadOnlyList<MapRoomGeometry> rooms,
    IReadOnlyList<MapRoomLink>? links = null,
    string? detailAssetBucket = null,
    IReadOnlyList<MapSectionDetailGeometry>? details = null
)
{
    public int Width { get; } = width;

    public int Height { get; } = height;

    public IReadOnlyList<MapRoomGeometry> Rooms { get; } = rooms;

    public IReadOnlyList<MapRoomLink> Links { get; } = links ?? [];

    public string? DetailAssetBucket { get; } = detailAssetBucket;

    public IReadOnlyList<MapSectionDetailGeometry> Details { get; } = details ?? [];
}

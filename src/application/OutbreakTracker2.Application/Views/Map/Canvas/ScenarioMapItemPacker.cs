using Avalonia;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal static class ScenarioMapItemPacker
{
    public static IReadOnlyList<ScenarioMapItemPlacement> CreateLayout(
        MapSectionGeometry section,
        IReadOnlyList<ScenarioItemSlotViewModel> items,
        IPolygonCirclePackingService circlePackingService
    )
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(circlePackingService);

        if (items.Count == 0)
            return [];

        List<ScenarioMapItemPlacement> placements = [];

        foreach (MapRoomGeometry room in section.Rooms)
        {
            List<ScenarioItemSlotViewModel> roomItems = [];
            foreach (ScenarioItemSlotViewModel item in items)
            {
                if (room.Matches(item.RoomId, MapAssetNameUtility.Slugify(item.RoomName)))
                    roomItems.Add(item);
            }

            if (roomItems.Count == 0)
                continue;

            IReadOnlyList<Point> vertices = GetVertices(room);
            PolygonCirclePackingResult result = circlePackingService.Pack(
                new PolygonCirclePackingRequest(vertices, roomItems.Count)
            );

            int placementCount = Math.Min(roomItems.Count, result.Circles.Count);
            for (int index = 0; index < placementCount; index++)
            {
                PackedCircle circle = result.Circles[index];
                placements.Add(
                    new ScenarioMapItemPlacement(roomItems[index], circle.CenterX, circle.CenterY, circle.Radius)
                );
            }
        }

        return placements;
    }

    public static async ValueTask<IReadOnlyList<ScenarioMapItemPlacement>> CreateLayoutAsync(
        MapSectionGeometry section,
        IReadOnlyList<ScenarioItemSlotViewModel> items,
        IPolygonCirclePackingService circlePackingService,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(circlePackingService);

        if (items.Count == 0)
            return [];

        List<ScenarioMapItemPlacement> placements = [];

        foreach (MapRoomGeometry room in section.Rooms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<ScenarioItemSlotViewModel> roomItems = [];
            foreach (ScenarioItemSlotViewModel item in items)
            {
                if (room.Matches(item.RoomId, MapAssetNameUtility.Slugify(item.RoomName)))
                    roomItems.Add(item);
            }

            if (roomItems.Count == 0)
                continue;

            IReadOnlyList<Point> vertices = GetVertices(room);
            PolygonCirclePackingResult result = await circlePackingService
                .PackAsync(new PolygonCirclePackingRequest(vertices, roomItems.Count), cancellationToken)
                .ConfigureAwait(false);

            int placementCount = Math.Min(roomItems.Count, result.Circles.Count);
            for (int index = 0; index < placementCount; index++)
            {
                PackedCircle circle = result.Circles[index];
                placements.Add(
                    new ScenarioMapItemPlacement(roomItems[index], circle.CenterX, circle.CenterY, circle.Radius)
                );
            }
        }

        return placements;
    }

    private static IReadOnlyList<Point> GetVertices(MapRoomGeometry room)
    {
        if (room.Shape is MapRoomShape.Rectangle)
        {
            int[] rectangle = room.Coordinates;
            double left = Math.Min(rectangle[0], rectangle[2]);
            double top = Math.Min(rectangle[1], rectangle[3]);
            double right = Math.Max(rectangle[0], rectangle[2]);
            double bottom = Math.Max(rectangle[1], rectangle[3]);

            return [new Point(left, top), new Point(right, top), new Point(right, bottom), new Point(left, bottom)];
        }

        int[] coordinates = room.Coordinates;
        List<Point> vertices = new(coordinates.Length / 2);
        for (int index = 0; index < coordinates.Length; index += 2)
        {
            vertices.Add(new Point(coordinates[index], coordinates[index + 1]));
        }

        return vertices;
    }
}

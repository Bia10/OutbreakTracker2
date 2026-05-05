using Avalonia;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Common;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Application.Views.Map.Canvas;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.UnitTests;

public sealed class ScenarioMapItemPackerTests
{
    private static readonly IPolygonCirclePackingService CirclePackingService = new PolygonCirclePackingService();

    [Test]
    public async Task CreateLayoutAsync_PacksRectangleRoomItems_WithoutOverlap()
    {
        MapSectionGeometry section = new(
            100,
            100,
            [new MapRoomGeometry(1, "test-room", MapRoomShape.Rectangle, "10,10,90,90")]
        );
        ScenarioItemSlotViewModel[] items = CreateItems(roomId: 1, roomName: "Test room", count: 6);

        IReadOnlyList<ScenarioMapItemPlacement> placements = await ScenarioMapItemPacker.CreateLayoutAsync(
            section,
            items,
            CirclePackingService
        );

        await Assert.That(placements.Count).IsEqualTo(6);

        for (int i = 0; i < placements.Count; i++)
        {
            ScenarioMapItemPlacement placement = placements[i];
            await Assert.That(placement.CenterX - placement.Radius >= 10).IsTrue();
            await Assert.That(placement.CenterX + placement.Radius <= 90).IsTrue();
            await Assert.That(placement.CenterY - placement.Radius >= 10).IsTrue();
            await Assert.That(placement.CenterY + placement.Radius <= 90).IsTrue();

            for (int j = i + 1; j < placements.Count; j++)
            {
                ScenarioMapItemPlacement other = placements[j];
                double dx = placement.CenterX - other.CenterX;
                double dy = placement.CenterY - other.CenterY;
                double minDistance = placement.Radius + other.Radius;
                await Assert.That((dx * dx) + (dy * dy) >= (minDistance * minDistance)).IsTrue();
            }
        }
    }

    [Test]
    public async Task CreateLayout_PacksPolygonRoomItems_InsideEndOfTheRoadWaitingRoom()
    {
        MapSectionGeometry section = EndOfTheRoadMapGeometry.CreateSections()[
            "end-of-the-road/umbrella-research-facility"
        ];
        MapRoomGeometry waitingRoom = section.Rooms.Single(room => room.RoomId == 1);
        ScenarioItemSlotViewModel[] items = CreateItems(roomId: 1, roomName: "Waiting room", count: 4);

        IReadOnlyList<ScenarioMapItemPlacement> placements = ScenarioMapItemPacker.CreateLayout(
            section,
            items,
            CirclePackingService
        );

        await Assert.That(placements.Count).IsEqualTo(4);

        foreach (ScenarioMapItemPlacement placement in placements)
        {
            await Assert
                .That(IsCircleInsideRoom(waitingRoom, placement.CenterX, placement.CenterY, placement.Radius))
                .IsTrue();
        }
    }

    private static ScenarioItemSlotViewModel[] CreateItems(byte roomId, string roomName, int count)
    {
        ScenarioItemSlotViewModel[] items = new ScenarioItemSlotViewModel[count];

        for (int index = 0; index < count; index++)
        {
            items[index] = new ScenarioItemSlotViewModel(
                new DecodedItem
                {
                    Id = (short)(index + 1),
                    TypeId = (short)(100 + index),
                    TypeName = $"Item {index + 1}",
                    Quantity = 1,
                    Present = 1,
                    RoomId = roomId,
                    RoomName = roomName,
                },
                CreateItemImageViewModel(),
                GameFile.FileOne,
                (byte)index
            );
        }

        return items;
    }

    private static bool IsCircleInsideRoom(MapRoomGeometry room, double centerX, double centerY, double radius)
    {
        if (room.Shape is MapRoomShape.Rectangle)
        {
            int[] coordinates = room.Coordinates;
            double left = Math.Min(coordinates[0], coordinates[2]);
            double top = Math.Min(coordinates[1], coordinates[3]);
            double right = Math.Max(coordinates[0], coordinates[2]);
            double bottom = Math.Max(coordinates[1], coordinates[3]);
            return centerX - radius >= left
                && centerX + radius <= right
                && centerY - radius >= top
                && centerY + radius <= bottom;
        }

        return IsPointInsidePolygon(room.Coordinates, centerX, centerY)
            && DistanceToPolygonBoundary(room.Coordinates, centerX, centerY) >= radius;
    }

    private static bool IsPointInsidePolygon(int[] coordinates, double x, double y)
    {
        bool inside = false;
        int pointCount = coordinates.Length / 2;

        for (int i = 0, j = pointCount - 1; i < pointCount; j = i++)
        {
            double xi = coordinates[i * 2];
            double yi = coordinates[(i * 2) + 1];
            double xj = coordinates[j * 2];
            double yj = coordinates[(j * 2) + 1];

            bool intersects = ((yi > y) != (yj > y)) && (x < ((xj - xi) * (y - yi) / (yj - yi)) + xi);
            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    private static double DistanceToPolygonBoundary(int[] coordinates, double x, double y)
    {
        double minDistanceSquared = double.MaxValue;
        int pointCount = coordinates.Length / 2;

        for (int index = 0; index < pointCount; index++)
        {
            int nextIndex = (index + 1) % pointCount;
            double x0 = coordinates[index * 2];
            double y0 = coordinates[(index * 2) + 1];
            double x1 = coordinates[nextIndex * 2];
            double y1 = coordinates[(nextIndex * 2) + 1];
            minDistanceSquared = Math.Min(minDistanceSquared, DistanceSquaredToSegment(x, y, x0, y0, x1, y1));
        }

        return Math.Sqrt(minDistanceSquared);
    }

    private static double DistanceSquaredToSegment(double px, double py, double x0, double y0, double x1, double y1)
    {
        double dx = x1 - x0;
        double dy = y1 - y0;
        if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
            return ((px - x0) * (px - x0)) + ((py - y0) * (py - y0));

        double t = (((px - x0) * dx) + ((py - y0) * dy)) / ((dx * dx) + (dy * dy));
        t = Math.Clamp(t, 0.0, 1.0);

        double closestX = x0 + (t * dx);
        double closestY = y0 + (t * dy);
        double offsetX = px - closestX;
        double offsetY = py - closestY;
        return (offsetX * offsetX) + (offsetY * offsetY);
    }

    private static ItemImageViewModel CreateItemImageViewModel() =>
        new(NullLogger<ItemImageViewModel>.Instance, new StubImageViewModelFactory());

    private sealed class StubImageViewModelFactory : IImageViewModelFactory
    {
        public ImageViewModel Create() =>
            new(NullLogger<ImageViewModel>.Instance, new StubTextureAtlasService(), new ImmediateDispatcherService());
    }

    private sealed class StubTextureAtlasService : ITextureAtlasService
    {
        private static readonly ITextureAtlas EmptyAtlas = new StubTextureAtlas();

        public ITextureAtlas GetAtlas(string name) => EmptyAtlas;

        public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases() => new Dictionary<string, ITextureAtlas>();

        public Task LoadAtlasesAsync() => Task.CompletedTask;
    }

    private sealed class StubTextureAtlas : ITextureAtlas
    {
        public Bitmap? Texture => null;

        public bool TryGetSourceRectangle(string name, out Rect rect)
        {
            rect = default;
            return false;
        }

        public Rect GetSourceRectangle(string name) => default;
    }

    private sealed class ImmediateDispatcherService : IDispatcherService
    {
        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action) => action();

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            TResult result = action();
            return Task.FromResult<TResult?>(result);
        }
    }
}

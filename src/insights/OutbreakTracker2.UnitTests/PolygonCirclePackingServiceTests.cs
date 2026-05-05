using Avalonia;
using OutbreakTracker2.Application.Views.Map.Canvas;

namespace OutbreakTracker2.UnitTests;

public sealed class PolygonCirclePackingServiceTests
{
    private static readonly IPolygonCirclePackingService Service = new PolygonCirclePackingService();

    [Test]
    public async Task PackAsync_FitsRequestedCirclesInsideRectanglePolygon()
    {
        PolygonCirclePackingResult result = await Service.PackAsync(
            new PolygonCirclePackingRequest(
                [new Point(10, 10), new Point(90, 10), new Point(90, 90), new Point(10, 90)],
                8
            )
        );

        await Assert.That(result.IsComplete).IsTrue();
        await Assert.That(result.Circles.Count).IsEqualTo(8);
        await Assert.That(result.Circles.All(circle => CircleFitsRectangle(circle, 10, 10, 90, 90))).IsTrue();
        await Assert.That(CirclesDoNotOverlap(result.Circles)).IsTrue();
    }

    [Test]
    public async Task Pack_FitsRequestedCirclesInsideWaitingRoomPolygon()
    {
        int[] coordinates = EndOfTheRoadMapGeometry
            .CreateSections()["end-of-the-road/umbrella-research-facility"]
            .Rooms.Single(room => room.RoomId == 1)
            .Coordinates;
        List<Point> vertices = [];
        for (int index = 0; index < coordinates.Length; index += 2)
        {
            vertices.Add(new Point(coordinates[index], coordinates[index + 1]));
        }

        PolygonCirclePackingResult result = Service.Pack(new PolygonCirclePackingRequest(vertices, 5));

        await Assert.That(result.IsComplete).IsTrue();
        await Assert.That(result.Circles.Count).IsEqualTo(5);
        await Assert.That(result.Circles.All(circle => CircleFitsPolygon(vertices, circle))).IsTrue();
        await Assert.That(CirclesDoNotOverlap(result.Circles)).IsTrue();
    }

    [Test]
    public async Task Pack_SingleCircleTargetsDeepInteriorPointInsideConcavePolygon()
    {
        IReadOnlyList<Point> polygon =
        [
            new Point(10, 10),
            new Point(90, 10),
            new Point(90, 35),
            new Point(45, 35),
            new Point(45, 90),
            new Point(10, 90),
        ];

        PolygonCirclePackingResult result = Service.Pack(new PolygonCirclePackingRequest(polygon, 1));

        await Assert.That(result.IsComplete).IsTrue();
        await Assert.That(result.Circles.Count).IsEqualTo(1);
        await Assert.That(CircleFitsPolygon(polygon, result.Circles[0])).IsTrue();
        await Assert.That(result.Circles[0].Radius).IsGreaterThan(10);
    }

    private static bool CirclesDoNotOverlap(IReadOnlyList<PackedCircle> circles)
    {
        for (int index = 0; index < circles.Count; index++)
        {
            for (int otherIndex = index + 1; otherIndex < circles.Count; otherIndex++)
            {
                PackedCircle left = circles[index];
                PackedCircle right = circles[otherIndex];
                double dx = left.CenterX - right.CenterX;
                double dy = left.CenterY - right.CenterY;
                double minimumDistance = left.Radius + right.Radius;
                if (((dx * dx) + (dy * dy)) + 0.01 < minimumDistance * minimumDistance)
                    return false;
            }
        }

        return true;
    }

    private static bool CircleFitsRectangle(
        PackedCircle circle,
        double left,
        double top,
        double right,
        double bottom
    ) =>
        circle.CenterX - circle.Radius >= left
        && circle.CenterX + circle.Radius <= right
        && circle.CenterY - circle.Radius >= top
        && circle.CenterY + circle.Radius <= bottom;

    private static bool CircleFitsPolygon(IReadOnlyList<Point> polygon, PackedCircle circle) =>
        IsPointInsidePolygon(polygon, circle.CenterX, circle.CenterY)
        && DistanceToPolygonBoundary(polygon, circle.CenterX, circle.CenterY) + 0.01 >= circle.Radius;

    private static bool IsPointInsidePolygon(IReadOnlyList<Point> polygon, double x, double y)
    {
        bool inside = false;

        for (int index = 0, previousIndex = polygon.Count - 1; index < polygon.Count; previousIndex = index++)
        {
            Point current = polygon[index];
            Point previous = polygon[previousIndex];
            bool intersects =
                ((current.Y > y) != (previous.Y > y))
                && (x < (((previous.X - current.X) * (y - current.Y)) / (previous.Y - current.Y)) + current.X);

            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    private static double DistanceToPolygonBoundary(IReadOnlyList<Point> polygon, double x, double y)
    {
        double minimumDistanceSquared = double.MaxValue;

        for (int index = 0; index < polygon.Count; index++)
        {
            Point current = polygon[index];
            Point next = polygon[(index + 1) % polygon.Count];
            minimumDistanceSquared = Math.Min(
                minimumDistanceSquared,
                DistanceSquaredToSegment(x, y, current.X, current.Y, next.X, next.Y)
            );
        }

        return Math.Sqrt(minimumDistanceSquared);
    }

    private static double DistanceSquaredToSegment(double px, double py, double x0, double y0, double x1, double y1)
    {
        double dx = x1 - x0;
        double dy = y1 - y0;
        if (Math.Abs(dx) <= double.Epsilon && Math.Abs(dy) <= double.Epsilon)
            return ((px - x0) * (px - x0)) + ((py - y0) * (py - y0));

        double t = (((px - x0) * dx) + ((py - y0) * dy)) / ((dx * dx) + (dy * dy));
        t = Math.Clamp(t, 0.0, 1.0);

        double closestX = x0 + (t * dx);
        double closestY = y0 + (t * dy);
        double offsetX = px - closestX;
        double offsetY = py - closestY;
        return (offsetX * offsetX) + (offsetY * offsetY);
    }
}

using Avalonia;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

public sealed record PolygonCirclePackingRequest(IReadOnlyList<Point> Vertices, int CircleCount);

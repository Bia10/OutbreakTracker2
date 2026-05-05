namespace OutbreakTracker2.Application.Views.Map.Canvas;

public sealed record PolygonCirclePackingResult(IReadOnlyList<PackedCircle> Circles, bool IsComplete);

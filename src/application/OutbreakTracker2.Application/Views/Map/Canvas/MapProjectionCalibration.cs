namespace OutbreakTracker2.Application.Views.Map.Canvas;

public sealed record MapProjectionCalibration
{
    private const double DefaultCoordinateRange = 25000.0;
    private const double MaxScale = 0.01;
    private const double MinScale = 0.000001;

    public static MapProjectionCalibration Default { get; } =
        new() { ScaleX = 1.0 / DefaultCoordinateRange, ScaleY = 1.0 / DefaultCoordinateRange };

    public double ScaleX { get; init; } = 1.0 / DefaultCoordinateRange;

    public double ScaleY { get; init; } = 1.0 / DefaultCoordinateRange;

    public double OffsetX { get; init; }

    public double OffsetY { get; init; }

    public MapProjectionCalibration WithOffsetDelta(double deltaX, double deltaY) =>
        this with
        {
            OffsetX = OffsetX + deltaX,
            OffsetY = OffsetY + deltaY,
        };

    public MapProjectionCalibration WithScaleMultiplier(double multiplier)
    {
        if (multiplier <= 0)
            return this;

        return this with
        {
            ScaleX = Math.Clamp(ScaleX * multiplier, MinScale, MaxScale),
            ScaleY = Math.Clamp(ScaleY * multiplier, MinScale, MaxScale),
        };
    }
}

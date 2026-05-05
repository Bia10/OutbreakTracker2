namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal sealed record MapProjectionCalibrationDocument
{
    public Dictionary<string, MapProjectionCalibration> Calibrations { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> CalibrationGroups { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

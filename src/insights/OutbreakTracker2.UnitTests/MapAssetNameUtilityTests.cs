using OutbreakTracker2.Application.Views.Map.Canvas;

namespace OutbreakTracker2.UnitTests;

public sealed class MapAssetNameUtilityTests
{
    [Test]
    public async Task GetCalibrationTargetKey_FallsBackToAssetKey_WhenNoGroupIsConfigured()
    {
        string calibrationKey = MapAssetNameUtility.GetCalibrationTargetKey(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "r0060200.png"),
            calibrationGroups: null
        );

        await Assert.That(calibrationKey).IsEqualTo("end-of-the-road/r0060200");
    }

    [Test]
    public async Task GetCalibrationTargetKey_UsesConfiguredGroup_WhenAssetIsMapped()
    {
        Dictionary<string, string> calibrationGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            ["end-of-the-road/r0060200"] = "end-of-the-road/central-ring",
        };

        string calibrationKey = MapAssetNameUtility.GetCalibrationTargetKey(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "r0060200.png"),
            calibrationGroups
        );

        await Assert.That(calibrationKey).IsEqualTo("end-of-the-road/central-ring");
    }
}

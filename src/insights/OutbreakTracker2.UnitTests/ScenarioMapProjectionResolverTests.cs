using OutbreakTracker2.Application.Views.Map.Canvas;

namespace OutbreakTracker2.UnitTests;

public sealed class ScenarioMapProjectionResolverTests
{
    [Test]
    public async Task TryProjectNormalizedPosition_UsesGroupCalibrationForVisibleActiveRoom()
    {
        string relativePath = Path.Combine("Assets", "Maps", "end-of-the-road", "default.png");
        MapProjectionCalibration calibration = MapProjectionCalibration.Default;

        bool projected = ScenarioMapProjectionResolver.TryProjectNormalizedPosition(
            "End of the road",
            relativePath,
            activeRoomId: 1,
            activeRoomName: "Waiting room",
            entityRoomId: 1,
            entityRoomName: "Waiting room",
            worldX: 12_500,
            worldY: 12_500,
            calibration,
            out double normalizedX,
            out double normalizedY
        );

        await Assert.That(projected).IsTrue();
        await Assert.That(normalizedX).IsGreaterThan(0.499999d);
        await Assert.That(normalizedX).IsLessThan(0.500001d);
        await Assert.That(normalizedY).IsGreaterThan(0.499999d);
        await Assert.That(normalizedY).IsLessThan(0.500001d);
    }

    [Test]
    public async Task TryProjectNormalizedPosition_UsesGroupCalibrationForOtherVisibleRoomInSameSection()
    {
        string relativePath = Path.Combine("Assets", "Maps", "end-of-the-road", "default.png");
        MapProjectionCalibration calibration = MapProjectionCalibration.Default;

        bool projected = ScenarioMapProjectionResolver.TryProjectNormalizedPosition(
            "End of the road",
            relativePath,
            activeRoomId: 1,
            activeRoomName: "Waiting room",
            entityRoomId: -1,
            entityRoomName: "Central passage 1",
            worldX: 12_500,
            worldY: 12_500,
            calibration,
            out double normalizedX,
            out double normalizedY
        );

        await Assert.That(projected).IsTrue();
        await Assert.That(normalizedX).IsGreaterThan(0.499999d);
        await Assert.That(normalizedX).IsLessThan(0.500001d);
        await Assert.That(normalizedY).IsGreaterThan(0.499999d);
        await Assert.That(normalizedY).IsLessThan(0.500001d);
    }

    [Test]
    public async Task TryProjectNormalizedPosition_ReturnsFalseWhenEntityRoomIsOutsideVisibleSection()
    {
        bool projected = ScenarioMapProjectionResolver.TryProjectNormalizedPosition(
            "End of the road",
            Path.Combine("Assets", "Maps", "end-of-the-road", "default.png"),
            activeRoomId: 1,
            activeRoomName: "Waiting room",
            entityRoomId: 28,
            entityRoomName: "Maintenance passage 1",
            worldX: 12_500,
            worldY: 12_500,
            MapProjectionCalibration.Default,
            out _,
            out _
        );

        await Assert.That(projected).IsFalse();
    }

    [Test]
    public async Task TryProjectNormalizedPosition_FallsBackToWholeCanvasWhenGeometryIsUnavailable()
    {
        bool projected = ScenarioMapProjectionResolver.TryProjectNormalizedPosition(
            string.Empty,
            relativePath: null,
            activeRoomId: -1,
            activeRoomName: string.Empty,
            entityRoomId: -1,
            entityRoomName: string.Empty,
            worldX: 12_500,
            worldY: 6_250,
            MapProjectionCalibration.Default,
            out double normalizedX,
            out double normalizedY
        );

        await Assert.That(projected).IsTrue();
        await Assert.That(normalizedX).IsGreaterThan(0.499999d);
        await Assert.That(normalizedX).IsLessThan(0.500001d);
        await Assert.That(normalizedY).IsGreaterThan(0.249999d);
        await Assert.That(normalizedY).IsLessThan(0.250001d);
    }
}

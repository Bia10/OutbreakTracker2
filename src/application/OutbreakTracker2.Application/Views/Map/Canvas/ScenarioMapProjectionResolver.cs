namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal static class ScenarioMapProjectionResolver
{
    public static bool TryProjectNormalizedPosition(
        string scenarioName,
        string? relativePath,
        short activeRoomId,
        string activeRoomName,
        short entityRoomId,
        string entityRoomName,
        float worldX,
        float worldY,
        MapProjectionCalibration calibration,
        out double normalizedX,
        out double normalizedY
    )
    {
        double groupNormalizedX = calibration.OffsetX + (worldX * calibration.ScaleX);
        double groupNormalizedY = calibration.OffsetY + (worldY * calibration.ScaleY);

        if (
            !ScenarioMapGeometryRenderer.TryResolveGeometry(
                scenarioName,
                relativePath,
                activeRoomId,
                activeRoomName,
                out MapSectionGeometry? section,
                out _
            )
        )
        {
            normalizedX = groupNormalizedX;
            normalizedY = groupNormalizedY;
            return true;
        }

        string entityRoomSlug = MapAssetNameUtility.Slugify(entityRoomName);
        if (!section!.Rooms.Any(room => room.Matches(entityRoomId, entityRoomSlug)))
        {
            normalizedX = 0;
            normalizedY = 0;
            return false;
        }

        normalizedX = groupNormalizedX;
        normalizedY = groupNormalizedY;
        return true;
    }
}

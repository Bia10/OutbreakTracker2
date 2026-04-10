using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Services.Tracking;

internal static class AlertRuleHelpers
{
    internal static string ResolveRoomName(int roomId, string scenarioName)
    {
        if (
            !string.IsNullOrEmpty(scenarioName)
            && EnumUtility.TryParseByValueOrMember(scenarioName, out Scenario scenarioEnum)
        )
            return scenarioEnum.GetRoomName(roomId);

        return $"Room {roomId}";
    }
}

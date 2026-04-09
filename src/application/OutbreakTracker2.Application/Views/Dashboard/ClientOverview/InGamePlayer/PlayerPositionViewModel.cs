using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;

public sealed partial class PlayerPositionViewModel : ObservableObject
{
    [ObservableProperty]
    private float _positionX;

    [ObservableProperty]
    private float _positionY;

    [ObservableProperty]
    private string _roomName = string.Empty;

    public void Update(float positionX, float positionY, short roomId, string scenarioName)
    {
        PositionX = positionX;
        PositionY = positionY;
        RoomName = GetRoomName(roomId, scenarioName);
    }

    private static string GetRoomName(short roomId, string scenarioName)
    {
        if (
            !string.IsNullOrEmpty(scenarioName)
            && EnumUtility.TryParseByValueOrMember(scenarioName, out Scenario scenarioEnum)
        )
            return scenarioEnum.GetRoomName(roomId);

        return $"Room ID: {roomId}";
    }
}

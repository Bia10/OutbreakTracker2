using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class PlayerPositionViewModel : ObservableObject
{
    [ObservableProperty]
    private float _positionX;

    [ObservableProperty]
    private float _positionY;

    [ObservableProperty]
    private string _roomName = string.Empty;

    private readonly IDataManager _dataManager;

    public PlayerPositionViewModel(IDataManager dataManager)
    {
        _dataManager = dataManager;
    }

    public void Update(
        float positionX,
        float positionY,
        short roomId)
    {
        PositionX = positionX;
        PositionY = positionY;
        RoomName = GetRoomName(roomId);
    }

    private string GetRoomName(short roomId)
    {
        string curScenarioName = _dataManager.InGameScenario.ScenarioName;
        if (!string.IsNullOrEmpty(curScenarioName) && EnumUtility.TryParseByValueOrMember(curScenarioName, out Scenario scenarioEnum))
            return scenarioEnum.GetRoomName(roomId);

        return $"Room ID: {roomId}";
    }
}
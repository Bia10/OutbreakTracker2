namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;

public sealed class ScenarioRoomGroupViewModel
{
    public string RoomName { get; }
    public IReadOnlyList<ScenarioItemSlotViewModel> Items { get; }

    public ScenarioRoomGroupViewModel(string roomName, IReadOnlyList<ScenarioItemSlotViewModel> items)
    {
        RoomName = roomName;
        Items = items;
    }
}

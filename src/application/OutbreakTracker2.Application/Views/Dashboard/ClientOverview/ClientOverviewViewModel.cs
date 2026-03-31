using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.EmbeddedGame;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoors;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayers;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoom;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlots;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview;

public class ClientOverviewViewModel(
    LobbySlotsViewModel lobbySlotsViewModel,
    LobbyRoomViewModel lobbyRoomViewModel,
    InGameScenarioViewModel inGameScenarioViewModel,
    InGamePlayersViewModel inGamePlayersViewModel,
    InGameEnemiesViewModel inGameEnemiesViewModel,
    InGameDoorsViewModel inGameDoorsViewModel,
    EmbeddedGameViewModel embeddedGameViewModel
) : ObservableObject
{
    public LobbySlotsViewModel LobbySlotsViewModel { get; } =
        lobbySlotsViewModel ?? throw new ArgumentNullException(nameof(lobbySlotsViewModel));

    public LobbyRoomViewModel LobbyRoomViewModel { get; } =
        lobbyRoomViewModel ?? throw new ArgumentNullException(nameof(lobbyRoomViewModel));

    public InGameScenarioViewModel InGameScenarioViewModel { get; } =
        inGameScenarioViewModel ?? throw new ArgumentNullException(nameof(inGameScenarioViewModel));

    public InGamePlayersViewModel InGamePlayersViewModel { get; } =
        inGamePlayersViewModel ?? throw new ArgumentNullException(nameof(inGamePlayersViewModel));

    public InGameEnemiesViewModel InGameEnemiesViewModel { get; } =
        inGameEnemiesViewModel ?? throw new ArgumentNullException(nameof(inGameEnemiesViewModel));

    public InGameDoorsViewModel InGameDoorsViewModel { get; } =
        inGameDoorsViewModel ?? throw new ArgumentNullException(nameof(inGameDoorsViewModel));

    public EmbeddedGameViewModel EmbeddedGameViewModel { get; } =
        embeddedGameViewModel ?? throw new ArgumentNullException(nameof(embeddedGameViewModel));
}

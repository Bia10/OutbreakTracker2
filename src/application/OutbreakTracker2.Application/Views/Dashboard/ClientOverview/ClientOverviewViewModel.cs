using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoors;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayers;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoom;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlots;
using System;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview;

public class ClientOverviewViewModel : ObservableObject
{
    public LobbySlotsViewModel LobbySlotsViewModel { get; }

    public LobbyRoomViewModel LobbyRoomViewModel { get; }

    public InGameScenarioViewModel InGameScenarioViewModel { get; }

    public InGamePlayersViewModel InGamePlayersViewModel { get; }

    public InGameEnemiesViewModel InGameEnemiesViewModel { get; }

    public InGameDoorsViewModel InGameDoorsViewModel { get; }

    public ClientOverviewViewModel(
        LobbySlotsViewModel lobbySlotsViewModel,
        LobbyRoomViewModel lobbyRoomViewModel,
        InGameScenarioViewModel inGameScenarioViewModel,
        InGamePlayersViewModel inGamePlayersViewModel,
        InGameEnemiesViewModel inGameEnemiesViewModel,
        InGameDoorsViewModel inGameDoorsViewModel)
    {
        LobbySlotsViewModel = lobbySlotsViewModel ?? throw new ArgumentNullException(nameof(lobbySlotsViewModel));
        LobbyRoomViewModel = lobbyRoomViewModel ?? throw new ArgumentNullException(nameof(lobbyRoomViewModel));
        InGameScenarioViewModel = inGameScenarioViewModel ?? throw new ArgumentNullException(nameof(inGameScenarioViewModel));
        InGamePlayersViewModel = inGamePlayersViewModel ?? throw new ArgumentNullException(nameof(inGamePlayersViewModel));
        InGameEnemiesViewModel = inGameEnemiesViewModel ?? throw new ArgumentNullException(nameof(inGameEnemiesViewModel));
        InGameDoorsViewModel = inGameDoorsViewModel ?? throw new ArgumentNullException(nameof(inGameDoorsViewModel));
    }
}

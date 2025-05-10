using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Debug;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameDoors;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayers;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoom;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlots;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview;

public class ClientOverviewViewModel : ObservableObject
{
    public DebugViewModel DebugViewModel { get; }

    public LobbySlotsViewModel LobbySlotsViewModel { get; }

    public LobbyRoomViewModel LobbyRoomViewModel { get; }

    public InGamePlayersViewModel InGamePlayersViewModel { get; }

    public InGameEnemiesViewModel InGameEnemiesViewModel { get; }

    public InGameDoorsViewModel InGameDoorsViewModel { get; }

    public ClientOverviewViewModel(
        DebugViewModel debugViewModel,
        LobbySlotsViewModel lobbySlotsViewModel,
        LobbyRoomViewModel lobbyRoomViewModel,
        InGamePlayersViewModel inGamePlayersViewModel,
        InGameEnemiesViewModel inGameEnemiesViewModel,
        InGameDoorsViewModel inGameDoorsViewModel)
    {
        DebugViewModel = debugViewModel ?? throw new ArgumentNullException(nameof(debugViewModel));
        LobbySlotsViewModel = lobbySlotsViewModel ?? throw new ArgumentNullException(nameof(lobbySlotsViewModel));
        LobbyRoomViewModel = lobbyRoomViewModel ?? throw new ArgumentNullException(nameof(lobbyRoomViewModel));
        InGamePlayersViewModel = inGamePlayersViewModel ?? throw new ArgumentNullException(nameof(inGamePlayersViewModel));
        InGameEnemiesViewModel = inGameEnemiesViewModel ?? throw new ArgumentNullException(nameof(inGameEnemiesViewModel));
        InGameDoorsViewModel = inGameDoorsViewModel ?? throw new ArgumentNullException(nameof(inGameDoorsViewModel));
    }
}

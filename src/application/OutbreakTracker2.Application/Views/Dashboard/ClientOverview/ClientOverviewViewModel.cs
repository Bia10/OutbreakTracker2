using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.EmbeddedGame;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoors;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayers;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoom;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlots;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview;

public sealed class ClientOverviewViewModel(
    LobbySlotsViewModel lobbySlotsViewModel,
    LobbyRoomViewModel lobbyRoomViewModel,
    InGameScenarioViewModel inGameScenarioViewModel,
    InGamePlayersViewModel inGamePlayersViewModel,
    InGameEnemiesViewModel inGameEnemiesViewModel,
    InGameDoorsViewModel inGameDoorsViewModel,
    EmbeddedGameViewModel embeddedGameViewModel
) : ObservableObject, IAsyncDisposable
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

    public async ValueTask DisposeAsync()
    {
        // IAsyncDisposable children
        await LobbySlotsViewModel.DisposeAsync().ConfigureAwait(false);
        await LobbyRoomViewModel.DisposeAsync().ConfigureAwait(false);
        await InGamePlayersViewModel.DisposeAsync().ConfigureAwait(false);
        await InGameDoorsViewModel.DisposeAsync().ConfigureAwait(false);

        // IDisposable children
        InGameScenarioViewModel.Dispose();
        InGameEnemiesViewModel.Dispose();
        EmbeddedGameViewModel.Dispose();
    }
}

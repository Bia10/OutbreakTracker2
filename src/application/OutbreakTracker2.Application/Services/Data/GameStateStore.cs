using OutbreakTracker2.Application.Comparers;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Data;

/// <summary>
/// In-memory reactive state store for all decoded game data.
/// Implements IDataObservableSource (push) and IDataSnapshot (pull).
/// DataManager owns this and writes to the internal ReactiveProperty fields.
/// Consumers subscribe via the interface observable properties which are computed once
/// (DistinctUntilChanged) and held as stable references.
/// </summary>
internal sealed class GameStateStore : IDataObservableSource, IDataSnapshot, IDisposable
{
    // Internal mutable state — written by DataManager
    internal readonly ReactiveProperty<DecodedDoor[]> DoorsState = new([]);
    internal readonly ReactiveProperty<DecodedEnemy[]> EnemiesState = new([]);
    internal readonly ReactiveProperty<DecodedInGamePlayer[]> InGamePlayersState = new([]);
    internal readonly ReactiveProperty<DecodedInGameScenario> InGameScenarioState = new(new DecodedInGameScenario());
    internal readonly ReactiveProperty<DecodedLobbyRoom> LobbyRoomState = new(new DecodedLobbyRoom());
    internal readonly ReactiveProperty<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersState = new([]);
    internal readonly ReactiveProperty<DecodedLobbySlot[]> LobbySlotsState = new([]);
    internal readonly ReactiveProperty<bool> IsAtLobbyState = new(false);

    // Pre-computed observable views (stable references — computed once in constructor)
    private readonly Observable<DecodedDoor[]> _doorsObservable;
    private readonly Observable<DecodedEnemy[]> _enemiesObservable;
    private readonly Observable<DecodedInGamePlayer[]> _inGamePlayersObservable;
    private readonly Observable<DecodedInGameScenario> _inGameScenarioObservable;
    private readonly Observable<DecodedLobbyRoom> _lobbyRoomObservable;
    private readonly Observable<DecodedLobbyRoomPlayer[]> _lobbyRoomPlayersObservable;
    private readonly Observable<DecodedLobbySlot[]> _lobbySlotsObservable;
    private readonly Observable<bool> _isAtLobbyObservable;

    public GameStateStore()
    {
        _doorsObservable = DoorsState.DistinctUntilChanged(new ArraySequenceComparer<DecodedDoor>());
        _enemiesObservable = EnemiesState.DistinctUntilChanged(new ArraySequenceComparer<DecodedEnemy>());
        _inGamePlayersObservable = InGamePlayersState.DistinctUntilChanged(
            new ArraySequenceComparer<DecodedInGamePlayer>()
        );
        _inGameScenarioObservable = InGameScenarioState.DistinctUntilChanged();
        _lobbyRoomObservable = LobbyRoomState.DistinctUntilChanged();
        _lobbyRoomPlayersObservable = LobbyRoomPlayersState.DistinctUntilChanged(
            new ArraySequenceComparer<DecodedLobbyRoomPlayer>()
        );
        _lobbySlotsObservable = LobbySlotsState.DistinctUntilChanged(new ArraySequenceComparer<DecodedLobbySlot>());
        _isAtLobbyObservable = IsAtLobbyState.DistinctUntilChanged();
    }

    // IDataObservableSource — stable references, same semantics as DataManager had
    Observable<DecodedDoor[]> IDataObservableSource.DoorsObservable => _doorsObservable;
    Observable<DecodedEnemy[]> IDataObservableSource.EnemiesObservable => _enemiesObservable;
    Observable<DecodedInGamePlayer[]> IDataObservableSource.InGamePlayersObservable => _inGamePlayersObservable;
    Observable<DecodedInGameScenario> IDataObservableSource.InGameScenarioObservable => _inGameScenarioObservable;
    Observable<DecodedLobbyRoom> IDataObservableSource.LobbyRoomObservable => _lobbyRoomObservable;
    Observable<DecodedLobbyRoomPlayer[]> IDataObservableSource.LobbyRoomPlayersObservable =>
        _lobbyRoomPlayersObservable;
    Observable<DecodedLobbySlot[]> IDataObservableSource.LobbySlotsObservable => _lobbySlotsObservable;
    Observable<bool> IDataObservableSource.IsAtLobbyObservable => _isAtLobbyObservable;

    // IDataSnapshot — direct value access
    DecodedDoor[] IDataSnapshot.Doors => DoorsState.Value;
    DecodedEnemy[] IDataSnapshot.Enemies => EnemiesState.Value;
    DecodedInGamePlayer[] IDataSnapshot.InGamePlayers => InGamePlayersState.Value;
    DecodedInGameScenario IDataSnapshot.InGameScenario => InGameScenarioState.Value;
    DecodedLobbyRoom IDataSnapshot.LobbyRoom => LobbyRoomState.Value;
    DecodedLobbyRoomPlayer[] IDataSnapshot.LobbyRoomPlayers => LobbyRoomPlayersState.Value;
    DecodedLobbySlot[] IDataSnapshot.LobbySlots => LobbySlotsState.Value;
    bool IDataSnapshot.IsAtLobby => IsAtLobbyState.Value;

    public void Dispose()
    {
        DoorsState.Dispose();
        EnemiesState.Dispose();
        InGamePlayersState.Dispose();
        InGameScenarioState.Dispose();
        LobbyRoomState.Dispose();
        LobbyRoomPlayersState.Dispose();
        LobbySlotsState.Dispose();
        IsAtLobbyState.Dispose();
    }
}

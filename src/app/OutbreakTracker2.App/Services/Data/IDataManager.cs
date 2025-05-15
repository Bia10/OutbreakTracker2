using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.PCSX2;
using R3;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.Data;

public interface IDataManager
{
    public DecodedDoor[] Doors { get; }

    public DecodedEnemy[] Enemies { get; }

    public DecodedInGamePlayer[] InGamePlayers { get; }

    public DecodedInGameScenario InGameScenario { get; }

    public DecodedLobbyRoom LobbyRoom { get; }

    public DecodedLobbyRoomPlayer[] LobbyRoomPlayers { get; }

    public DecodedLobbySlot[] LobbySlots { get; }

    public Observable<DecodedDoor[]> DoorsObservable { get; }

    public Observable<DecodedEnemy[]> EnemiesObservable { get; }

    public Observable<DecodedInGamePlayer[]> InGamePlayersObservable { get; }

    public Observable<DecodedInGameScenario> InGameScenarioObservable { get; }

    public Observable<DecodedLobbyRoom> LobbyRoomObservable { get; }

    public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable { get; }

    public Observable<DecodedLobbySlot[]> LobbySlotsObservable { get; }

    public void UpdateDoors();

    public void UpdateEnemies();

    public void UpdateInGamePlayer();

    public void UpdateInGameScenario();

    public void UpdateLobbyRoom();

    public void UpdateLobbyRoomPlayers();

    public void UpdateLobbySlots();

    public ValueTask InitializeAsync(GameClient gameClient, CancellationToken cancellationToken);
}
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class TrackerRegistry : ITrackerRegistry, IDisposable
{
    public IEntityTracker<DecodedEnemy> Enemies { get; }
    public IEntityTracker<DecodedDoor> Doors { get; }
    public IEntityTracker<DecodedInGamePlayer> Players { get; }
    public IEntityTracker<DecodedLobbyRoomPlayer> LobbyPlayers { get; }
    public IEntityTracker<DecodedLobbySlot> LobbySlots { get; }

    public Observable<AlertNotification> AllAlerts { get; }

    public TrackerRegistry(IDataManager dataManager)
    {
        Enemies = new EntityTracker<DecodedEnemy>(new EntityChangeSource<DecodedEnemy>(dataManager.EnemiesObservable));
        Doors = new EntityTracker<DecodedDoor>(new EntityChangeSource<DecodedDoor>(dataManager.DoorsObservable));
        Players = new EntityTracker<DecodedInGamePlayer>(
            new EntityChangeSource<DecodedInGamePlayer>(dataManager.InGamePlayersObservable)
        );
        LobbyPlayers = new EntityTracker<DecodedLobbyRoomPlayer>(
            new EntityChangeSource<DecodedLobbyRoomPlayer>(dataManager.LobbyRoomPlayersObservable)
        );
        LobbySlots = new EntityTracker<DecodedLobbySlot>(
            new EntityChangeSource<DecodedLobbySlot>(dataManager.LobbySlotsObservable)
        );

        DefaultPlayerAlertRules.Register(Players);
        DefaultEnemyAlertRules.Register(Enemies);
        DefaultDoorAlertRules.Register(Doors);
        DefaultLobbySlotAlertRules.Register(LobbySlots);
        DefaultLobbyPlayerAlertRules.Register(LobbyPlayers);

        AllAlerts = Observable.Merge(
            Enemies.Alerts,
            Doors.Alerts,
            Players.Alerts,
            LobbyPlayers.Alerts,
            LobbySlots.Alerts
        );
    }

    public void Dispose()
    {
        Enemies.Dispose();
        Doors.Dispose();
        Players.Dispose();
        LobbyPlayers.Dispose();
        LobbySlots.Dispose();
    }
}

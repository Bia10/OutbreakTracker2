using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed class TrackerRegistry : ITrackerRegistry, IDisposable
{
    public IEntityTracker<DecodedEnemy> Enemies { get; }
    public IEntityTracker<DecodedDoor> Doors { get; }
    public IEntityTracker<DecodedInGamePlayer> Players { get; }
    public IEntityTracker<DecodedLobbySlot> LobbySlots { get; }

    public Observable<AlertNotification> AllAlerts { get; }

    public TrackerRegistry(
        IDataObservableSource dataObservable,
        IDataSnapshot dataSnapshot,
        IAppSettingsService settingsService
    )
    {
        Enemies = new EntityTracker<DecodedEnemy>(
            new EntityChangeSource<DecodedEnemy>(dataObservable.EnemiesObservable)
        );
        Doors = new EntityTracker<DecodedDoor>(new EntityChangeSource<DecodedDoor>(dataObservable.DoorsObservable));
        Players = new EntityTracker<DecodedInGamePlayer>(
            new EntityChangeSource<DecodedInGamePlayer>(dataObservable.InGamePlayersObservable)
        );
        LobbySlots = new EntityTracker<DecodedLobbySlot>(
            new EntityChangeSource<DecodedLobbySlot>(dataObservable.LobbySlotsObservable)
        );

        Observable<AlertNotification> lobbySlotAlerts = LobbySlots.Alerts;
        Observable<AlertNotification> gatedLobbyAlerts = lobbySlotAlerts
            .WithLatestFrom(
                dataObservable.IsAtLobbyObservable,
                static (alert, isAtLobby) => (Alert: alert, IsAtLobby: isAtLobby)
            )
            .Where(static state => state.IsAtLobby)
            .Select(static state => state.Alert);

        DefaultPlayerAlertRules.Register(Players, settingsService, dataSnapshot);
        DefaultEnemyAlertRules.Register(Enemies, settingsService, dataSnapshot);
        DefaultDoorAlertRules.Register(Doors, settingsService, dataSnapshot);
        DefaultLobbySlotAlertRules.Register(LobbySlots, settingsService);

        AllAlerts = Observable.Merge(Enemies.Alerts, Doors.Alerts, Players.Alerts, gatedLobbyAlerts);
    }

    public void Dispose()
    {
        Enemies.Dispose();
        Doors.Dispose();
        Players.Dispose();
        LobbySlots.Dispose();
    }
}

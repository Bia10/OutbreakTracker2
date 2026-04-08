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

        RegisterDefaultPlayerRules(Players);
        RegisterDefaultEnemyRules(Enemies);

        AllAlerts = Observable.Merge(
            Enemies.Alerts,
            Doors.Alerts,
            Players.Alerts,
            LobbyPlayers.Alerts,
            LobbySlots.Alerts
        );
    }

    private static void RegisterDefaultEnemyRules(IEntityTracker<DecodedEnemy> enemies)
    {
        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) => cur.Enabled != 0 && cur.MaxHp > 0 && cur.SlotId > 0 && (prev?.Enabled == 0),
                cur => new AlertNotification(
                    "Mob Spawned",
                    $"{cur.Name} spawned in room {cur.RoomId}!",
                    AlertLevel.Info
                )
            )
        );
    }

    private static void RegisterDefaultPlayerRules(IEntityTracker<DecodedInGamePlayer> players)
    {
        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                    cur.Condition.Equals("danger", StringComparison.OrdinalIgnoreCase)
                    && !(prev?.Condition.Equals("danger", StringComparison.OrdinalIgnoreCase) ?? false),
                cur => new AlertNotification("Condition Update", $"{cur.Name} is now in DANGER!", AlertLevel.Error)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                    cur.Condition.Equals("gas", StringComparison.OrdinalIgnoreCase)
                    && !(prev?.Condition.Equals("gas", StringComparison.OrdinalIgnoreCase) ?? false),
                cur => new AlertNotification("Condition Update", $"{cur.Name} is gassed!", AlertLevel.Warning)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                    string.Equals(cur.Status, "Dead", StringComparison.Ordinal)
                    && !string.Equals(prev?.Status, "Dead", StringComparison.Ordinal),
                cur => new AlertNotification("Status Update", $"{cur.Name} has DIED!", AlertLevel.Error)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                    string.Equals(cur.Status, "Zombie", StringComparison.Ordinal)
                    && !string.Equals(prev?.Status, "Zombie", StringComparison.Ordinal),
                cur => new AlertNotification("Status Update", $"{cur.Name} turned into a ZOMBIE!", AlertLevel.Error)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                    string.Equals(cur.Status, "Down", StringComparison.Ordinal)
                    && !string.Equals(prev?.Status, "Down", StringComparison.Ordinal),
                cur => new AlertNotification("Status Update", $"{cur.Name} is now DOWNED!", AlertLevel.Warning)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                    string.Equals(cur.Status, "Bleed", StringComparison.Ordinal)
                    && !string.Equals(prev?.Status, "Bleed", StringComparison.Ordinal),
                cur => new AlertNotification("Status Update", $"{cur.Name} is now BLEEDING!", AlertLevel.Warning)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) =>
                    cur.CurHealth <= 0
                    && (prev?.CurHealth ?? 0) > 0
                    && !string.Equals(cur.Status, "Zombie", StringComparison.Ordinal),
                cur => new AlertNotification("Player Died", $"{cur.Name} health dropped to 0!", AlertLevel.Error)
            )
        );
    }

    public void Dispose()
    {
        (Enemies as IDisposable)?.Dispose();
        (Doors as IDisposable)?.Dispose();
        (Players as IDisposable)?.Dispose();
        (LobbyPlayers as IDisposable)?.Dispose();
        (LobbySlots as IDisposable)?.Dispose();
    }
}

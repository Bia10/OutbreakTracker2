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
        RegisterDefaultDoorRules(Doors);
        RegisterDefaultLobbySlotRules(LobbySlots);
        RegisterDefaultLobbyPlayerRules(LobbyPlayers);

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

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) => cur.CurHp == 0 && (prev?.CurHp ?? 0) > 0 && prev?.Enabled != 0,
                cur => new AlertNotification(
                    "Mob Killed",
                    $"{cur.Name} in room {cur.RoomId} was killed!",
                    AlertLevel.Info
                )
            )
        );

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) => cur.Enabled == 0 && (prev?.Enabled ?? 0) != 0,
                cur => new AlertNotification(
                    "Mob Despawned",
                    $"{cur.Name} despawned from room {cur.RoomId}.",
                    AlertLevel.Info
                )
            )
        );

        enemies.AddRule(
            new PredicateAlertRule<DecodedEnemy>(
                (cur, prev) => cur.RoomId != (prev?.RoomId ?? cur.RoomId) && cur.Enabled != 0,
                cur => new AlertNotification(
                    "Mob Room Change",
                    $"{cur.Name} moved to room {cur.RoomId}.",
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

        // Virus threshold crossings
        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) => cur.VirusPercentage >= 50.0 && (prev?.VirusPercentage ?? 0.0) < 50.0,
                cur => new AlertNotification(
                    "Virus Warning",
                    $"{cur.Name} virus is at {cur.VirusPercentage:F1}%!",
                    AlertLevel.Warning
                )
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) => cur.VirusPercentage >= 75.0 && (prev?.VirusPercentage ?? 0.0) < 75.0,
                cur => new AlertNotification(
                    "Virus Critical",
                    $"{cur.Name} virus is at {cur.VirusPercentage:F1}%!",
                    AlertLevel.Error
                )
            )
        );

        // Antivirus/AntivirusG timers running out
        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) => cur.AntiVirusTime == 0 && (prev?.AntiVirusTime ?? 0) > 0,
                cur => new AlertNotification(
                    "Antivirus Expired",
                    $"{cur.Name}'s antivirus ran out!",
                    AlertLevel.Warning
                )
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) => cur.AntiVirusGTime == 0 && (prev?.AntiVirusGTime ?? 0) > 0,
                cur => new AlertNotification(
                    "Antivirus-G Expired",
                    $"{cur.Name}'s antivirus-G ran out!",
                    AlertLevel.Warning
                )
            )
        );

        // Bleed timer expired
        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) => cur.BleedTime == 0 && (prev?.BleedTime ?? 0) > 0,
                cur => new AlertNotification("Bleed Stopped", $"{cur.Name}'s bleed timer expired.", AlertLevel.Info)
            )
        );

        // Room change while in-game
        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) => cur.IsInGame && prev is not null && cur.RoomId != prev.RoomId,
                cur => new AlertNotification("Room Change", $"{cur.Name} moved to room {cur.RoomId}.", AlertLevel.Info)
            )
        );

        // Player joined / left game
        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) => cur.IsInGame && !(prev?.IsInGame ?? true),
                cur => new AlertNotification("Player Joined", $"{cur.Name} joined the game.", AlertLevel.Info)
            )
        );

        players.AddRule(
            new PredicateAlertRule<DecodedInGamePlayer>(
                (cur, prev) => !cur.IsInGame && (prev?.IsInGame ?? false),
                cur => new AlertNotification("Player Left", $"{cur.Name} left the game.", AlertLevel.Warning)
            )
        );
    }

    private static void RegisterDefaultDoorRules(IEntityTracker<DecodedDoor> doors)
    {
        doors.AddRule(
            new PredicateAlertRule<DecodedDoor>(
                (cur, prev) => prev is not null && cur.Flag != prev.Flag,
                cur => new AlertNotification(
                    "Door Flag Changed",
                    $"Door state changed (flag: {cur.Flag}).",
                    AlertLevel.Info
                )
            )
        );

        doors.AddRule(
            new PredicateAlertRule<DecodedDoor>(
                (cur, prev) => cur.Hp == 0 && (prev?.Hp ?? 0) > 0,
                cur => new AlertNotification("Door Destroyed", "A door was destroyed!", AlertLevel.Warning)
            )
        );

        doors.AddRule(
            new PredicateAlertRule<DecodedDoor>(
                (cur, prev) => prev is not null && !string.Equals(cur.Status, prev.Status, StringComparison.Ordinal),
                cur => new AlertNotification(
                    "Door Status Changed",
                    $"Door status is now {cur.Status}.",
                    AlertLevel.Info
                )
            )
        );
    }

    private static void RegisterDefaultLobbySlotRules(IEntityTracker<DecodedLobbySlot> lobbySlots)
    {
        lobbySlots.AddRule(
            new PredicateAlertRule<DecodedLobbySlot>(
                (cur, prev) => cur.IsPassProtected && !(prev?.IsPassProtected ?? false),
                cur => new AlertNotification(
                    "Lobby Password Set",
                    $"Slot {cur.SlotNumber} \"{cur.Title}\" is now password-protected.",
                    AlertLevel.Info
                )
            )
        );

        lobbySlots.AddRule(
            new PredicateAlertRule<DecodedLobbySlot>(
                (cur, prev) =>
                    cur.MaxPlayers > 0 && cur.CurPlayers >= cur.MaxPlayers && (prev?.CurPlayers ?? 0) < cur.MaxPlayers,
                cur => new AlertNotification(
                    "Lobby Full",
                    $"Slot {cur.SlotNumber} \"{cur.Title}\" is full ({cur.CurPlayers}/{cur.MaxPlayers}).",
                    AlertLevel.Info
                )
            )
        );

        lobbySlots.AddRule(
            new PredicateAlertRule<DecodedLobbySlot>(
                (cur, prev) => prev is not null && !string.Equals(cur.Status, prev.Status, StringComparison.Ordinal),
                cur => new AlertNotification(
                    "Lobby Status Changed",
                    $"Slot {cur.SlotNumber} \"{cur.Title}\" status is now {cur.Status}.",
                    AlertLevel.Info
                )
            )
        );
    }

    private static void RegisterDefaultLobbyPlayerRules(IEntityTracker<DecodedLobbyRoomPlayer> lobbyPlayers)
    {
        lobbyPlayers.AddRule(
            new PredicateAlertRule<DecodedLobbyRoomPlayer>(
                (cur, prev) => cur.IsEnabled && !(prev?.IsEnabled ?? false),
                cur => new AlertNotification(
                    "Player Joined Lobby",
                    $"{cur.CharacterName} joined the lobby.",
                    AlertLevel.Info
                )
            )
        );

        lobbyPlayers.AddRule(
            new PredicateAlertRule<DecodedLobbyRoomPlayer>(
                (cur, prev) => !cur.IsEnabled && (prev?.IsEnabled ?? false),
                cur => new AlertNotification(
                    "Player Left Lobby",
                    $"{cur.CharacterName} left the lobby.",
                    AlertLevel.Info
                )
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

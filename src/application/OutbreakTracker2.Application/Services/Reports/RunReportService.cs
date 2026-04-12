using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed class RunReportService : IRunReportService
{
    private readonly ILogger<RunReportService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IRunReportWriter _writer;
    private readonly Subject<RunEvent> _eventSubject = new();
    private readonly Subject<RunReport> _completedReports = new();
    private readonly IDisposable _subscription;

    // _activePlayers and _lastScenario/_lastScenarioId are accessed from two independent
    // thread-pool polling loops (player diffs vs. scenario diffs). ConcurrentDictionary
    // handles concurrent reads/writes/clears on _activePlayers. volatile on the reference
    // fields ensures the latest write is visible across threads.
    private readonly ConcurrentDictionary<Ulid, DecodedInGamePlayer> _activePlayers = new();
    private readonly DecodedInGamePlayer?[] _activePlayersBySlot = new DecodedInGamePlayer?[GameConstants.MaxPlayers];
    private volatile string _lastScenarioId = string.Empty;
    private volatile DecodedInGameScenario? _lastScenario;
    private volatile ScenarioStatus _lastScenarioStatus = ScenarioStatus.None;
    private DecodedItem[]? _prevItems;

    // Protected by _sessionLock — may be read from external threads via IsRunning.
    private readonly Lock _sessionLock = new();
    private List<RunEvent>? _sessionEvents;
    private string _currentScenarioId = string.Empty;
    private DateTimeOffset _sessionStartedAt;

    public bool IsRunning
    {
        get
        {
            lock (_sessionLock)
                return _sessionEvents is not null;
        }
    }

    public Observable<RunEvent> Events => _eventSubject;
    public Observable<RunReport> CompletedReports => _completedReports;

    public RunReportService(
        ITrackerRegistry trackerRegistry,
        IDataManager dataManager,
        IRunReportWriter writer,
        TimeProvider timeProvider,
        ILogger<RunReportService> logger
    )
    {
        _writer = writer;
        _timeProvider = timeProvider;
        _logger = logger;

        IDisposable playerSub = trackerRegistry.Players.Changes.Diffs.Subscribe(ProcessPlayerDiff);
        IDisposable enemySub = trackerRegistry.Enemies.Changes.Diffs.Subscribe(ProcessEnemyDiff);
        IDisposable doorSub = trackerRegistry.Doors.Changes.Diffs.Subscribe(ProcessDoorDiff);
        IDisposable lobbySub = trackerRegistry.LobbySlots.Changes.Diffs.Subscribe(TrackScenarioId);
        IDisposable itemSub = dataManager.InGameScenarioObservable.Subscribe(ProcessScenarioDiff);

        _subscription = Disposable.Combine(playerSub, enemySub, doorSub, lobbySub, itemSub);
    }

    private void TrackScenarioId(CollectionDiff<DecodedLobbySlot> diff)
    {
        foreach (DecodedLobbySlot slot in diff.Added)
            if (!string.IsNullOrEmpty(slot.ScenarioId))
                _lastScenarioId = slot.ScenarioId;

        foreach (EntityChange<DecodedLobbySlot> change in diff.Changed)
            if (!string.IsNullOrEmpty(change.Current.ScenarioId))
                _lastScenarioId = change.Current.ScenarioId;
    }

    private void AutoStartSession()
    {
        lock (_sessionLock)
        {
            if (_sessionEvents is not null)
                return;

            _currentScenarioId = _lastScenarioId;
            _sessionStartedAt = _timeProvider.GetUtcNow();
            _sessionEvents = [];
        }

        string playerSummary;
        if (_activePlayers.Count == 0)
        {
            playerSummary = "(none)";
        }
        else
        {
            System.Text.StringBuilder sb = new();
            bool first = true;
            foreach (DecodedInGamePlayer p in _activePlayers.Values)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append(p.Name)
                    .Append(" HP:")
                    .Append(p.CurHealth)
                    .Append('/')
                    .Append(p.MaxHealth)
                    .Append(System.Globalization.CultureInfo.InvariantCulture, $" Virus:{p.VirusPercentage:F1}%");
                first = false;
            }

            playerSummary = sb.ToString();
        }

        _logger.LogInformation(
            "Scenario tracking started. Scenario: {ScenarioName} | Difficulty: {Difficulty} | Players ({PlayerCount}): {Players}",
            string.IsNullOrEmpty(_lastScenario?.ScenarioName) ? _currentScenarioId : _lastScenario.ScenarioName,
            string.IsNullOrEmpty(_lastScenario?.Difficulty) ? "unknown" : _lastScenario.Difficulty,
            _activePlayers.Count,
            playerSummary
        );
    }

    private void AutoStopSession()
    {
        List<RunEvent> events;
        string scenarioId;
        string scenarioName = _lastScenario?.ScenarioName ?? string.Empty;
        DateTimeOffset startedAt;
        DateTimeOffset endedAt = _timeProvider.GetUtcNow();

        lock (_sessionLock)
        {
            if (_sessionEvents is null)
                return;

            events = _sessionEvents;
            scenarioId = _currentScenarioId;
            startedAt = _sessionStartedAt;
            _sessionEvents = null;
        }

        Scenario scenario = EnumUtility.TryParseByValueOrMember<Scenario>(scenarioName, out Scenario parsed)
            ? parsed
            : Scenario.Unknown;

        RunReport report = new(Ulid.NewUlid(), scenarioId, scenarioName, scenario, startedAt, endedAt, events);

        RunReportStats stats = report.ComputeStats();
        _logger.LogInformation(
            "Scenario tracking ended. Scenario: {ScenarioName} | Duration: {Duration} | Events: {EventCount} | "
                + "Kills: {Kills} | DamageTaken: {DamageTaken} | PeakVirus: {PeakVirus:F1}%",
            string.IsNullOrEmpty(scenarioName) ? scenarioId : scenarioName,
            report.Duration,
            events.Count,
            stats.TotalEnemyKills,
            stats.TotalDamageTaken,
            stats.PeakVirusPercentage
        );

        _completedReports.OnNext(report);

        // Fire-and-forget — do not block the polling thread.
        _ = _writer
            .WriteAsync(report)
            .ContinueWith(
                t =>
                    _logger.LogError(
                        t.Exception,
                        "Failed to persist run report for session {SessionId}",
                        report.SessionId
                    ),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default
            );
    }

    private void Emit(RunEvent evt)
    {
        RunEvent annotated = evt with { ScenarioFrame = _lastScenario?.FrameCounter ?? 0 };
        lock (_sessionLock)
            _sessionEvents?.Add(annotated);

        _eventSubject.OnNext(annotated);
    }

    // Returns all active in-game players currently in the same room as the given enemy room ID,
    // including their Power stat for weighted damage attribution.
    // Called from ProcessEnemyDiff, which runs on the same polling thread as ProcessPlayerDiff,
    // so _activePlayers is safe to read without locking.
    private IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> FindContributingPlayers(byte enemyRoomId)
    {
        short roomId = (short)enemyRoomId;
        List<(Ulid, string, float)>? result = null;

        foreach (DecodedInGamePlayer player in _activePlayers.Values)
        {
            if (player.RoomId != roomId)
                continue;

            result ??= [];
            result.Add((player.Id, player.Name, player.Power));
        }

        return result ?? [];
    }

    private void ProcessPlayerDiff(CollectionDiff<DecodedInGamePlayer> diff)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        bool hadActivePlayers = _activePlayers.Count > 0;
        ScenarioStatus currentScenarioStatus = _lastScenarioStatus;

        // Update active-player snapshot before emitting events so FindContributingPlayers
        // sees the current room positions and lifecycle checks see the new counts.
        foreach (DecodedInGamePlayer player in diff.Added)
            if (player.IsInGame)
            {
                _activePlayers[player.Id] = player;
                _activePlayersBySlot[player.SlotIndex] = player;
            }

        foreach (EntityChange<DecodedInGamePlayer> change in diff.Changed)
        {
            if (change.Current.IsInGame)
            {
                _activePlayers[change.Current.Id] = change.Current;
                _activePlayersBySlot[change.Current.SlotIndex] = change.Current;
            }
            else
            {
                _activePlayers.TryRemove(change.Current.Id, out _);
                _activePlayersBySlot[change.Current.SlotIndex] = null;
            }
        }

        // During room transitions the game clears player memory temporarily.
        // Keep stale player data rather than evicting and restarting the session.
        bool isTransitional = IsTransitionalStatus(currentScenarioStatus);

        HashSet<Ulid>? removedActiveIds = null;
        if (!isTransitional)
            foreach (DecodedInGamePlayer player in diff.Removed)
            {
                if (_activePlayers.TryRemove(player.Id, out _))
                {
                    _activePlayersBySlot[player.SlotIndex] = null;
                    removedActiveIds ??= [];
                    removedActiveIds.Add(player.Id);
                }
            }

        bool hasActivePlayers = _activePlayers.Count > 0;

        // Auto-start before join events so they are captured inside the session.
        // Only start when actually in-game — this prevents a ghost session firing
        // immediately after GameFinished clears _activePlayers and ProcessPlayerDiff
        // re-adds them from the same diff tick.
        if (!hadActivePlayers && hasActivePlayers && currentScenarioStatus == ScenarioStatus.InGame)
            AutoStartSession();

        foreach (DecodedInGamePlayer player in diff.Added)
        {
            if (!player.IsInGame)
                continue;

            Emit(
                new PlayerJoinedEvent(
                    now,
                    player.Id,
                    player.Name,
                    player.CurHealth,
                    player.MaxHealth,
                    player.VirusPercentage
                )
            );
        }

        foreach (EntityChange<DecodedInGamePlayer> change in diff.Changed)
        {
            DecodedInGamePlayer prev = change.Previous;
            DecodedInGamePlayer curr = change.Current;

            if (prev.CurHealth != curr.CurHealth)
                Emit(
                    new PlayerHealthChangedEvent(
                        now,
                        curr.Id,
                        curr.Name,
                        prev.CurHealth,
                        curr.CurHealth,
                        curr.MaxHealth
                    )
                );

            if (!string.Equals(prev.Condition, curr.Condition, StringComparison.Ordinal))
                Emit(new PlayerConditionChangedEvent(now, curr.Id, curr.Name, prev.Condition, curr.Condition));

            if (prev.VirusPercentage != curr.VirusPercentage)
                Emit(new PlayerVirusChangedEvent(now, curr.Id, curr.Name, prev.VirusPercentage, curr.VirusPercentage));

            if (!string.Equals(prev.Status, curr.Status, StringComparison.Ordinal))
                Emit(new PlayerStatusChangedEvent(now, curr.Id, curr.Name, prev.Status, curr.Status));

            if (curr.IsInGame && prev.RoomId != curr.RoomId)
                Emit(new PlayerRoomChangedEvent(now, curr.Id, curr.Name, prev.RoomId, curr.RoomId));

            EmitEffectChange(now, curr, "Bleed", prev.BleedTime, curr.BleedTime);
            EmitEffectChange(now, curr, "Herb", prev.HerbTime, curr.HerbTime);
            EmitEffectChange(now, curr, "AntiVirus", prev.AntiVirusTime, curr.AntiVirusTime);
            EmitEffectChange(now, curr, "AntiVirusG", prev.AntiVirusGTime, curr.AntiVirusGTime);

            EmitInventoryChanges(now, curr, prev.Inventory, curr.Inventory, InventoryKind.Main);
            EmitInventoryChanges(now, curr, prev.SpecialInventory, curr.SpecialInventory, InventoryKind.Special);
            EmitInventoryChanges(now, curr, prev.DeadInventory, curr.DeadInventory, InventoryKind.Dead);
            EmitInventoryChanges(
                now,
                curr,
                prev.SpecialDeadInventory,
                curr.SpecialDeadInventory,
                InventoryKind.SpecialDead
            );
        }

        if (removedActiveIds is not null)
            foreach (DecodedInGamePlayer player in diff.Removed)
                if (removedActiveIds.Contains(player.Id))
                    Emit(new PlayerLeftEvent(now, player.Id, player.Name, player.CurHealth, player.VirusPercentage));

        // Auto-stop after leave events so they are captured inside the session.
        // Do NOT stop during transient loading states — player data is temporarily absent.
        if (hadActivePlayers && !hasActivePlayers && !isTransitional)
            AutoStopSession();
    }

    // PickedUp encodes the player slot as a 1-based index (1 = slot 0, 2 = slot 1, …).
    // Returns the player's name if the slot is known, otherwise a fallback label.
    private string ResolvePickupHolderName(short pickedUp)
    {
        int slotIndex = pickedUp - 1;
        if (slotIndex >= 0 && slotIndex < _activePlayersBySlot.Length)
        {
            DecodedInGamePlayer? player = _activePlayersBySlot[slotIndex];
            if (player is not null && !string.IsNullOrEmpty(player.Name))
                return player.Name;
        }

        return $"P{pickedUp}";
    }

    private void EmitEffectChange(
        DateTimeOffset now,
        DecodedInGamePlayer player,
        string effectName,
        ushort prevTime,
        ushort currTime
    )
    {
        if (prevTime == 0 && currTime > 0)
            Emit(new PlayerEffectChangedEvent(now, player.Id, player.Name, effectName, IsApplied: true));
        else if (prevTime > 0 && currTime == 0)
            Emit(new PlayerEffectChangedEvent(now, player.Id, player.Name, effectName, IsApplied: false));
    }

    private void EmitInventoryChanges(
        DateTimeOffset now,
        DecodedInGamePlayer player,
        byte[] prev,
        byte[] curr,
        InventoryKind kind
    )
    {
        int count = Math.Min(prev.Length, curr.Length);
        for (int i = 0; i < count; i++)
        {
            if (prev[i] == curr[i])
                continue;

            string oldName = EnumUtility.GetEnumString(prev[i], ItemType.Unknown);
            string newName = EnumUtility.GetEnumString(curr[i], ItemType.Unknown);
            Emit(new PlayerInventoryChangedEvent(now, player.Id, player.Name, kind, i, oldName, newName));
        }
    }

    private void ProcessEnemyDiff(CollectionDiff<DecodedEnemy> diff)
    {
        if (_lastScenarioStatus != ScenarioStatus.InGame)
            return;

        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (DecodedEnemy enemy in diff.Added)
            Emit(new EnemySpawnedEvent(now, enemy.Id, enemy.Name, enemy.SlotId, enemy.RoomId, enemy.MaxHp));

        foreach (DecodedEnemy enemy in diff.Removed)
        {
            if (enemy.CurHp <= 1 && !AlertRuleHelpers.IsInvulnerableEnemy(enemy.NameId, enemy.MaxHp))
                Emit(
                    new EnemyKilledEvent(
                        now,
                        enemy.Id,
                        enemy.Name,
                        enemy.SlotId,
                        enemy.RoomId,
                        FindContributingPlayers(enemy.RoomId)
                    )
                );
            else
                Emit(
                    new EnemyDespawnedEvent(
                        now,
                        enemy.Id,
                        enemy.Name,
                        enemy.SlotId,
                        enemy.RoomId,
                        enemy.CurHp,
                        enemy.MaxHp
                    )
                );
        }

        foreach (EntityChange<DecodedEnemy> change in diff.Changed)
        {
            DecodedEnemy prev = change.Previous;
            DecodedEnemy curr = change.Current;

            // Kill detection: HP transitioned from the alive range into the dead range.
            // The game marks death at CurHp == 0x0 or CurHp >= 0x8000 (0xffff is the most
            // common), matching GetEnemiesHealthStatusStringForFileTwo's dead HP cases.
            // Guards against slot reuse between rooms: NameId must match (same entity type)
            // and curr.NameId != 0 (non-empty slot). This is the primary kill path because
            // the fixed 80-slot array keeps the same Ulid per slot so deaths surface here
            // as diff.Changed, never as diff.Removed during live play.
            bool prevAliveHp = prev.CurHp > 0 && prev.CurHp < 0x8000;
            bool currDeadHp = curr.CurHp == 0 || curr.CurHp >= 0x8000;
            if (
                prevAliveHp
                && currDeadHp
                && prev.Enabled != 0
                && curr.NameId != 0
                && prev.NameId == curr.NameId
                && !AlertRuleHelpers.IsInvulnerableEnemy(prev.NameId, prev.MaxHp)
            )
            {
                Emit(
                    new EnemyKilledEvent(
                        now,
                        prev.Id,
                        prev.Name,
                        prev.SlotId,
                        prev.RoomId,
                        FindContributingPlayers(prev.RoomId)
                    )
                );
                continue;
            }

            // Scripted despawn: slot was disabled by the engine while HP was still positive.
            if (prev.Enabled != 0 && curr.Enabled == 0 && curr.CurHp > 0)
            {
                Emit(
                    new EnemyDespawnedEvent(now, prev.Id, prev.Name, prev.SlotId, prev.RoomId, prev.CurHp, prev.MaxHp)
                );
                continue;
            }

            if (prev.CurHp > curr.CurHp)
                Emit(
                    new EnemyDamagedEvent(
                        now,
                        curr.Id,
                        curr.Name,
                        curr.SlotId,
                        curr.RoomId,
                        prev.CurHp,
                        curr.CurHp,
                        curr.MaxHp,
                        FindContributingPlayers(curr.RoomId)
                    )
                );

            if (prev.Status != curr.Status)
                Emit(
                    new EnemyStatusChangedEvent(
                        now,
                        curr.Id,
                        curr.Name,
                        curr.SlotId,
                        curr.RoomId,
                        prev.Status,
                        curr.Status,
                        FindContributingPlayers(curr.RoomId)
                    )
                );

            if (curr.Enabled != 0 && prev.RoomId != curr.RoomId)
                Emit(new EnemyRoomChangedEvent(now, curr.Id, curr.Name, curr.SlotId, prev.RoomId, curr.RoomId));
        }
    }

    private static readonly IReadOnlySet<ScenarioStatus> _monitoredStatuses = new HashSet<ScenarioStatus>
    {
        ScenarioStatus.Unknown8,
        ScenarioStatus.Unknown9,
        ScenarioStatus.Unknown10,
        ScenarioStatus.Unknown11,
    };

    // Statuses where player memory is being reloaded and data reads are unreliable.
    // During these states we preserve the last known player snapshot instead of evicting.
    // Unknown8-11 are observed in online sessions between room transitions; they must be
    // treated as transitional to prevent spurious session splits.
    private static bool IsTransitionalStatus(ScenarioStatus status) =>
        status
            is ScenarioStatus.TransitionLoading
                or ScenarioStatus.CinematicPlaying
                or ScenarioStatus.GenericLoading
                or ScenarioStatus.PostIntroLoading
                or ScenarioStatus.Unknown8
                or ScenarioStatus.Unknown9
                or ScenarioStatus.Unknown10
                or ScenarioStatus.Unknown11;

    private void ProcessScenarioDiff(DecodedInGameScenario scenario)
    {
        _lastScenario = scenario;

        ScenarioStatus previousStatus = _lastScenarioStatus;
        _lastScenarioStatus = scenario.Status;

        if (previousStatus != _lastScenarioStatus)
        {
            if (_monitoredStatuses.Contains(_lastScenarioStatus))
                Emit(new ScenarioStatusChangedEvent(_timeProvider.GetUtcNow(), previousStatus, _lastScenarioStatus));

            // Also trigger auto-start here so it fires even if ProcessPlayerDiff already
            // populated _activePlayers before the InGame status was observed.
            if (_lastScenarioStatus == ScenarioStatus.InGame && _activePlayers.Count > 0)
                AutoStartSession();

            if (_lastScenarioStatus is ScenarioStatus.GameFinished or ScenarioStatus.RankScreen)
            {
                DateTimeOffset finishTime = _timeProvider.GetUtcNow();
                foreach (DecodedInGamePlayer player in _activePlayers.Values)
                    Emit(
                        new PlayerLeftEvent(
                            finishTime,
                            player.Id,
                            player.Name,
                            player.CurHealth,
                            player.VirusPercentage
                        )
                    );
                _activePlayers.Clear();
                AutoStopSession();
            }

            // Unexpected termination: game returned to None without going through GameFinished.
            if (_lastScenarioStatus == ScenarioStatus.None && IsRunning)
            {
                _activePlayers.Clear();
                AutoStopSession();
            }
        }

        DecodedItem[] curr = scenario.Items;
        DecodedItem[]? prev = _prevItems;
        _prevItems = curr;

        if (prev is null || _lastScenarioStatus != ScenarioStatus.InGame)
            return;

        DateTimeOffset now = _timeProvider.GetUtcNow();
        int count = Math.Min(prev.Length, curr.Length);

        for (int i = 0; i < count; i++)
        {
            DecodedItem p = prev[i];
            DecodedItem c = curr[i];

            // GetItems() may return a pre-allocated array with default-valued struct elements if
            // memory is not yet mapped (early in the PCSX2 startup sequence); skip uninitialized slots.
            if (string.IsNullOrEmpty(p.TypeName) || string.IsNullOrEmpty(c.TypeName))
                continue;

            if (p.PickedUp == 0 && c.PickedUp > 0)
            {
                string holderName = ResolvePickupHolderName(c.PickedUp);
                Emit(new ItemPickedUpEvent(now, c.TypeName, c.RoomId, holderName));
            }
            else if (p.PickedUp > 0 && c.PickedUp == 0)
            {
                string prevHolderName = ResolvePickupHolderName(p.PickedUp);
                Emit(new ItemDroppedEvent(now, c.TypeName, c.RoomId, prevHolderName));
            }

            if (p.Quantity != c.Quantity)
                Emit(new ItemQuantityChangedEvent(now, c.TypeName, c.SlotIndex, c.RoomId, p.Quantity, c.Quantity));
        }
    }

    private void ProcessDoorDiff(CollectionDiff<DecodedDoor> diff)
    {
        if (_lastScenarioStatus != ScenarioStatus.InGame)
            return;

        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (EntityChange<DecodedDoor> change in diff.Changed)
        {
            if (!string.Equals(change.Previous.Status, change.Current.Status, StringComparison.Ordinal))
                Emit(
                    new DoorStateChangedEvent(
                        now,
                        change.Current.Id,
                        change.Current.SlotId,
                        change.Previous.Status,
                        change.Current.Status
                    )
                );

            if (change.Previous.Hp != change.Current.Hp)
                Emit(
                    new DoorDamagedEvent(
                        now,
                        change.Current.Id,
                        change.Current.SlotId,
                        change.Previous.Hp,
                        change.Current.Hp
                    )
                );

            if (change.Previous.Flag != change.Current.Flag)
                Emit(
                    new DoorFlagChangedEvent(
                        now,
                        change.Current.Id,
                        change.Current.SlotId,
                        change.Previous.Flag,
                        change.Current.Flag
                    )
                );
        }
    }

    public void Dispose()
    {
        _subscription.Dispose();
        _eventSubject.Dispose();
        _completedReports.Dispose();
    }
}

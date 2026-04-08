using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Models;
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

    // Modified only on the polling thread — no lock needed.
    private readonly Dictionary<Ulid, DecodedInGamePlayer> _activePlayers = [];
    private string _lastScenarioId = string.Empty;
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

        _logger.LogInformation("Run report session auto-started for scenario: {ScenarioId}", _currentScenarioId);
    }

    private void AutoStopSession()
    {
        List<RunEvent> events;
        string scenarioId;
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

        RunReport report = new(Ulid.NewUlid(), scenarioId, startedAt, endedAt, events);

        _logger.LogInformation(
            "Run report session auto-stopped. {EventCount} events captured over {Duration}",
            events.Count,
            report.Duration
        );

        _completedReports.OnNext(report);

        // Fire-and-forget — do not block the polling thread.
        _ = _writer.WriteAsync(report);
    }

    private void Emit(RunEvent evt)
    {
        lock (_sessionLock)
            _sessionEvents?.Add(evt);

        _eventSubject.OnNext(evt);
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

        // Update active-player snapshot before emitting events so FindContributingPlayers
        // sees the current room positions and lifecycle checks see the new counts.
        foreach (DecodedInGamePlayer player in diff.Added)
            if (player.IsInGame)
                _activePlayers[player.Id] = player;

        foreach (EntityChange<DecodedInGamePlayer> change in diff.Changed)
        {
            if (change.Current.IsInGame)
                _activePlayers[change.Current.Id] = change.Current;
            else
                _activePlayers.Remove(change.Current.Id);
        }

        foreach (DecodedInGamePlayer player in diff.Removed)
            _activePlayers.Remove(player.Id);

        bool hasActivePlayers = _activePlayers.Count > 0;

        // Auto-start before join events so they are captured inside the session.
        if (!hadActivePlayers && hasActivePlayers)
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
        }

        foreach (DecodedInGamePlayer player in diff.Removed)
            Emit(new PlayerLeftEvent(now, player.Id, player.Name, player.CurHealth, player.VirusPercentage));

        // Auto-stop after leave events so they are captured inside the session.
        if (hadActivePlayers && !hasActivePlayers)
            AutoStopSession();
    }

    private void ProcessEnemyDiff(CollectionDiff<DecodedEnemy> diff)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (DecodedEnemy enemy in diff.Added)
            Emit(new EnemySpawnedEvent(now, enemy.Id, enemy.Name, enemy.SlotId, enemy.RoomId, enemy.MaxHp));

        foreach (DecodedEnemy enemy in diff.Removed)
        {
            if (enemy.CurHp == 0)
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
            if (change.Previous.CurHp > change.Current.CurHp)
                Emit(
                    new EnemyDamagedEvent(
                        now,
                        change.Current.Id,
                        change.Current.Name,
                        change.Current.SlotId,
                        change.Current.RoomId,
                        change.Previous.CurHp,
                        change.Current.CurHp,
                        change.Current.MaxHp,
                        FindContributingPlayers(change.Current.RoomId)
                    )
                );

            if (change.Previous.Status != change.Current.Status)
                Emit(
                    new EnemyStatusChangedEvent(
                        now,
                        change.Current.Id,
                        change.Current.Name,
                        change.Current.SlotId,
                        change.Current.RoomId,
                        change.Previous.Status,
                        change.Current.Status,
                        FindContributingPlayers(change.Current.RoomId)
                    )
                );
        }
    }

    private void ProcessScenarioDiff(DecodedInGameScenario scenario)
    {
        DecodedItem[] curr = scenario.Items;
        DecodedItem[]? prev = _prevItems;
        _prevItems = curr;

        if (prev is null)
            return;

        DateTimeOffset now = _timeProvider.GetUtcNow();
        int count = Math.Min(prev.Length, curr.Length);

        for (int i = 0; i < count; i++)
        {
            DecodedItem p = prev[i];
            DecodedItem c = curr[i];

            if (p.PickedUp == 0 && c.PickedUp > 0)
                Emit(new ItemPickedUpEvent(now, c.TypeName, c.RoomId, c.PickedUpByName));
            else if (p.PickedUp > 0 && c.PickedUp == 0)
                Emit(new ItemDroppedEvent(now, c.TypeName, c.RoomId, p.PickedUpByName));
        }
    }

    private void ProcessDoorDiff(CollectionDiff<DecodedDoor> diff)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (EntityChange<DecodedDoor> change in diff.Changed)
        {
            if (!string.Equals(change.Previous.Status, change.Current.Status, StringComparison.Ordinal))
                Emit(new DoorStateChangedEvent(now, change.Current.Id, change.Previous.Status, change.Current.Status));

            if (change.Previous.Hp != change.Current.Hp)
                Emit(new DoorDamagedEvent(now, change.Current.Id, change.Previous.Hp, change.Current.Hp));
        }
    }

    public void Dispose()
    {
        _subscription.Dispose();
        _eventSubject.Dispose();
        _completedReports.Dispose();
    }
}

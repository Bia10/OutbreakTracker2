using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed class RunReportService : IRunReportService
{
    private readonly ILogger<RunReportService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IToastService _toastService;
    private readonly IRunReportWriter _writer;
    private readonly IRunReportCollectionDiffProcessor<DecodedLobbySlot> _lobbySlotDiffProcessor;
    private readonly IRunReportCollectionDiffProcessor<DecodedInGamePlayer> _playerDiffProcessor;
    private readonly IRunReportCollectionDiffProcessor<DecodedEnemy> _enemyDiffProcessor;
    private readonly IRunReportCollectionDiffProcessor<DecodedDoor> _doorDiffProcessor;
    private readonly IRunReportScenarioProcessor _scenarioProcessor;
    private readonly Subject<RunEvent> _eventSubject = new();
    private readonly Subject<RunReport> _completedReports = new();
    private readonly IDisposable _subscription;
    private readonly RunReportProcessingState _processingState = new();
    private readonly RunReportProcessingContext _processingContext;

    private volatile Scenario _currentScenario = Scenario.Unknown;

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
        IToastService toastService,
        TimeProvider timeProvider,
        ILogger<RunReportService> logger
    )
        : this(
            trackerRegistry,
            dataManager,
            writer,
            toastService,
            timeProvider,
            logger,
            new RunReportLobbySlotDiffProcessor(),
            new RunReportPlayerDiffProcessor(),
            new RunReportEnemyDiffProcessor(logger),
            new RunReportDoorDiffProcessor(),
            new RunReportScenarioProcessor()
        ) { }

    internal RunReportService(
        ITrackerRegistry trackerRegistry,
        IDataManager dataManager,
        IRunReportWriter writer,
        IToastService toastService,
        TimeProvider timeProvider,
        ILogger<RunReportService> logger,
        IRunReportCollectionDiffProcessor<DecodedLobbySlot> lobbySlotDiffProcessor,
        IRunReportCollectionDiffProcessor<DecodedInGamePlayer> playerDiffProcessor,
        IRunReportCollectionDiffProcessor<DecodedEnemy> enemyDiffProcessor,
        IRunReportCollectionDiffProcessor<DecodedDoor> doorDiffProcessor,
        IRunReportScenarioProcessor scenarioProcessor
    )
    {
        _writer = writer;
        _toastService = toastService;
        _timeProvider = timeProvider;
        _logger = logger;
        _lobbySlotDiffProcessor = lobbySlotDiffProcessor;
        _playerDiffProcessor = playerDiffProcessor;
        _enemyDiffProcessor = enemyDiffProcessor;
        _doorDiffProcessor = doorDiffProcessor;
        _scenarioProcessor = scenarioProcessor;

        _processingContext = new RunReportProcessingContext(
            _processingState,
            timeProvider,
            Emit,
            AutoStartSession,
            AutoStopSession,
            () => IsRunning,
            FindContributingPlayers,
            ResolvePickupHolderName
        );

        IDisposable playerSub = trackerRegistry.PlayerChanges.Diffs.Subscribe(diff =>
            _playerDiffProcessor.Process(diff, _processingContext)
        );
        IDisposable enemySub = trackerRegistry.EnemyChanges.Diffs.Subscribe(diff =>
            _enemyDiffProcessor.Process(diff, _processingContext)
        );
        IDisposable doorSub = trackerRegistry.DoorChanges.Diffs.Subscribe(diff =>
            _doorDiffProcessor.Process(diff, _processingContext)
        );
        IDisposable lobbySub = trackerRegistry.LobbySlotChanges.Diffs.Subscribe(diff =>
            _lobbySlotDiffProcessor.Process(diff, _processingContext)
        );
        IDisposable itemSub = dataManager.InGameScenarioObservable.Subscribe(scenario =>
            _scenarioProcessor.Process(scenario, _processingContext)
        );

        _subscription = Disposable.Combine(playerSub, enemySub, doorSub, lobbySub, itemSub);
    }

    private void AutoStartSession()
    {
        lock (_sessionLock)
        {
            if (_sessionEvents is not null)
                return;

            _currentScenarioId = _processingState.LastScenarioId;
            _sessionStartedAt = _timeProvider.GetUtcNow();
            _sessionEvents = [];
        }

        _currentScenario = EnumUtility.TryParseByValueOrMember<Scenario>(
            _processingState.LastScenario?.ScenarioName ?? string.Empty,
            out Scenario parsedScenario
        )
            ? parsedScenario
            : Scenario.Unknown;

        string playerSummary;
        if (_processingState.ActivePlayers.Count == 0)
        {
            playerSummary = "(none)";
        }
        else
        {
            System.Text.StringBuilder sb = new();
            bool first = true;
            foreach (DecodedInGamePlayer p in _processingState.ActivePlayers.Values)
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
            string.IsNullOrEmpty(_processingState.LastScenario?.ScenarioName)
                ? _currentScenarioId
                : _processingState.LastScenario.ScenarioName,
            string.IsNullOrEmpty(_processingState.LastScenario?.Difficulty)
                ? "unknown"
                : _processingState.LastScenario.Difficulty,
            _processingState.ActivePlayers.Count,
            playerSummary
        );
    }

    private void AutoStopSession()
    {
        List<RunEvent> events;
        string scenarioId;
        string scenarioName = _processingState.LastScenario?.ScenarioName ?? string.Empty;
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

        RunReport report = new(Ulid.NewUlid(), scenarioId, scenarioName, _currentScenario, startedAt, endedAt, events);

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
        _ = WriteReportSafeAsync(report);
    }

    private async Task WriteReportSafeAsync(RunReport report)
    {
        try
        {
            await _writer.WriteAsync(report).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist one or more run report artifacts for session {SessionId}",
                report.SessionId
            );

            NotifyReportWriteFailure(report);
        }
    }

    private void NotifyReportWriteFailure(RunReport report)
    {
        string scenarioLabel = string.IsNullOrWhiteSpace(report.ScenarioName) ? report.ScenarioId : report.ScenarioName;

        try
        {
            Task toastTask = _toastService.InvokeErrorToastAsync(
                $"Could not save the run report for {scenarioLabel}. Check the logs for details.",
                "Run report export failed"
            );

            _ = toastTask.ContinueWith(
                task =>
                    _logger.LogError(
                        task.Exception,
                        "Failed to show run-report write failure toast for session {SessionId}",
                        report.SessionId
                    ),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to dispatch run-report write failure toast for session {SessionId}",
                report.SessionId
            );
        }
    }

    private void Emit(RunEvent evt)
    {
        RunEvent annotated = evt with { ScenarioFrame = _processingState.LastScenario?.FrameCounter ?? 0 };
        lock (_sessionLock)
            _sessionEvents?.Add(annotated);

        _eventSubject.OnNext(annotated);
    }

    // Returns all active in-game players currently in the same room as the given enemy room ID,
    // including their Power stat for weighted damage attribution.
    // Called by the enemy diff processor while the player processor maintains the active-player snapshot.
    private IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> FindContributingPlayers(byte enemyRoomId)
    {
        short roomId = (short)enemyRoomId;
        List<(Ulid, string, float)>? result = null;

        foreach (DecodedInGamePlayer player in _processingState.ActivePlayers.Values)
        {
            if (player.RoomId != roomId)
                continue;

            result ??= [];
            result.Add((player.Id, player.Name, player.Power));
        }

        return result ?? [];
    }

    // PickedUp encodes the player slot as a 1-based index (1 = slot 0, 2 = slot 1, …).
    // Returns the player's name if the slot is known, otherwise an empty string so the caller
    // can emit an anonymous event rather than inventing a fake player name.
    private string ResolvePickupHolderName(short pickedUp)
    {
        int slotIndex = pickedUp - 1;
        if (slotIndex >= 0 && slotIndex < _processingState.ActivePlayersBySlot.Length)
        {
            DecodedInGamePlayer? player = _processingState.ActivePlayersBySlot[slotIndex];
            if (player is not null && !string.IsNullOrEmpty(player.Name))
                return player.Name;
        }

        return string.Empty;
    }

    public void Dispose()
    {
        _subscription.Dispose();
        _eventSubject.Dispose();
        _completedReports.Dispose();
    }
}

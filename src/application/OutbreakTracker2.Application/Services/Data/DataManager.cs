using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Extensions;
using OutbreakTracker2.MemoryWatcherIntegration;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using R3;

namespace OutbreakTracker2.Application.Services.Data;

public sealed class DataManager : IDataManager, ICurrentScenarioState, IDisposable
{
    private const int InitializeStateIdle = 0;
    private const int InitializeStateInitializing = 1;
    private const int InitializeStateInitialized = 2;
    private static readonly TimeSpan UpdateLoopShutdownTimeout = TimeSpan.FromSeconds(2);
    private const OutbreakTrackerMemoryDomains InGameDomains =
        OutbreakTrackerMemoryDomains.Scenario
        | OutbreakTrackerMemoryDomains.InGamePlayers
        | OutbreakTrackerMemoryDomains.Enemies
        | OutbreakTrackerMemoryDomains.Doors;
    private const OutbreakTrackerMemoryDomains LobbyDomains =
        OutbreakTrackerMemoryDomains.LobbyRoom
        | OutbreakTrackerMemoryDomains.LobbyRoomPlayers
        | OutbreakTrackerMemoryDomains.LobbySlots;
    private int _disposeState;
    private int _initializeState;
    private readonly ILogger<DataManager> _logger;
    private readonly IEEmemMemory _eememMemory;
    private readonly IMemoryActivitySource _memoryActivitySource;
    private readonly IGameReaderFactory _readerFactory;
    private Task? _updateLoopTask;
    private IDisposable? _loggingSubscriptions;
    private IDisposable? _processSubscription;
    private CancellationTokenSource? _updateCts;

    private IDoorReader? _doorReader;
    private IEnemiesReader? _enemiesReader;
    private IInGamePlayerReader? _inGamePlayerReader;
    private IInGameScenarioReader? _inGameScenarioReader;
    private ILobbyRoomPlayerReader? _lobbyRoomPlayerReader;
    private ILobbyRoomReader? _lobbyRoomReader;
    private ILobbySlotReader? _lobbySlotReader;

    private readonly GameStateStore _store = new();

    private volatile bool _wasInScenario;

    private IDataObservableSource Store => _store;
    public Observable<DecodedDoor[]> DoorsObservable => Store.DoorsObservable;
    public Observable<DecodedEnemy[]> EnemiesObservable => Store.EnemiesObservable;
    public Observable<DecodedInGamePlayer[]> InGamePlayersObservable => Store.InGamePlayersObservable;
    public Observable<InGameOverviewSnapshot> InGameOverviewObservable => Store.InGameOverviewObservable;
    public Observable<DecodedInGameScenario> InGameScenarioObservable => Store.InGameScenarioObservable;
    public Observable<DecodedLobbyRoom> LobbyRoomObservable => Store.LobbyRoomObservable;
    public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable => Store.LobbyRoomPlayersObservable;
    public Observable<DecodedLobbySlot[]> LobbySlotsObservable => Store.LobbySlotsObservable;
    public Observable<bool> IsAtLobbyObservable => Store.IsAtLobbyObservable;

    public DecodedDoor[] Doors => _store.DoorsState.Value;
    public DecodedEnemy[] Enemies => _store.EnemiesState.Value;
    public DecodedInGamePlayer[] InGamePlayers => _store.InGamePlayersState.Value;
    public DecodedInGameScenario InGameScenario => _store.InGameScenarioState.Value;
    public DecodedLobbyRoom LobbyRoom => _store.LobbyRoomState.Value;
    public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _store.LobbyRoomPlayersState.Value;
    public DecodedLobbySlot[] LobbySlots => _store.LobbySlotsState.Value;
    public bool IsAtLobby => _store.IsAtLobbyState.Value;

    // ICurrentScenarioState — narrow pull contract for alert rules
    string ICurrentScenarioState.ScenarioName => _store.InGameScenarioState.Value.ScenarioName;
    ScenarioStatus ICurrentScenarioState.Status => _store.InGameScenarioState.Value.Status;

    private readonly TimeSpan _fastUpdateInterval;
    private readonly TimeSpan _slowUpdateInterval;

    public DataManager(
        ILogger<DataManager> logger,
        IEEmemMemory eememMemory,
        IMemoryActivitySource memoryActivitySource,
        IProcessLauncher processLauncher,
        IGameReaderFactory readerFactory,
        DataManagerOptions options
    )
    {
        _logger = logger;
        _eememMemory = eememMemory;
        _memoryActivitySource = memoryActivitySource;
        _readerFactory = readerFactory;
        ArgumentNullException.ThrowIfNull(options);

        _fastUpdateInterval = CreateUpdateInterval(options.FastUpdateIntervalMs, nameof(options.FastUpdateIntervalMs));
        _slowUpdateInterval = CreateUpdateInterval(options.SlowUpdateIntervalMs, nameof(options.SlowUpdateIntervalMs));

        _processSubscription = processLauncher
            .ProcessUpdate.Where(model => !model.IsRunning)
            .Subscribe(_ => StopUpdateLoops());

        SetupObservablesLogging();
    }

    private static TimeSpan CreateUpdateInterval(int intervalMilliseconds, string optionName)
    {
        if (intervalMilliseconds <= 0)
            throw new InvalidOperationException($"DataManager option '{optionName}' must be greater than zero.");

        return TimeSpan.FromMilliseconds(intervalMilliseconds);
    }

    private static string GetProcessNameForDiagnostics(IGameClient gameClient)
    {
        return gameClient.Process.GetSafeName() ?? "unknown";
    }

    private static int GetProcessIdForDiagnostics(IGameClient gameClient)
    {
        Process? process = gameClient.Process;
        return process is null ? -1 : process.GetSafeId();
    }

    private static void CancelAndDispose(CancellationTokenSource? cancellationTokenSource)
    {
        if (cancellationTokenSource is null)
            return;

        try
        {
            cancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Another teardown path already won the race and disposed the CTS.
        }

        cancellationTokenSource.Dispose();
    }

    private void SetupObservablesLogging()
    {
        // Add subscriptions to a list incrementally so any exception during Subscribe()
        // allows the catch block to dispose the already-registered subscriptions instead
        // of leaking them (unlike a batch array initializer where partial failures are silent).
        List<IDisposable> subs = new(5);
        try
        {
            subs.Add(
                DoorsObservable
                    .Do(doors => _logger.LogTrace("Doors data changed: {Count} doors.", doors.Length))
                    .Subscribe()
            );
            subs.Add(
                EnemiesObservable
                    .Do(enemies => _logger.LogTrace("Enemies data changed: {Count} enemies.", enemies.Length))
                    .Subscribe()
            );
            subs.Add(
                InGamePlayersObservable
                    .Do(players => _logger.LogTrace("InGamePlayers data changed: {Count} players.", players.Length))
                    .Subscribe()
            );
            subs.Add(
                InGameScenarioObservable
                    .Select(scenario => scenario.Status)
                    .DistinctUntilChanged()
                    .Do(status => _logger.LogInformation("InGameScenario STATUS CHANGED: {Status}", status))
                    .Subscribe()
            );
            subs.Add(
                LobbyRoomObservable
                    .Select(lobbyRoom => lobbyRoom.Status)
                    .DistinctUntilChanged()
                    .Do(status => _logger.LogInformation("LobbyRoom STATUS CHANGED: {Status}", status))
                    .Subscribe()
            );
            _loggingSubscriptions = Disposable.Combine([.. subs]);
        }
        catch
        {
            foreach (IDisposable s in subs)
                s.Dispose();
            throw;
        }
    }

    public async ValueTask InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(gameClient);

        if (
            Interlocked.CompareExchange(ref _initializeState, InitializeStateInitializing, InitializeStateIdle)
            != InitializeStateIdle
        )
        {
            _logger.LogWarning(
                "DataManager is already initialized or initialization is in progress. Skipping re-initialization."
            );
            return;
        }

        try
        {
            _logger.LogInformation("Attempting to initialize DataManager and EEmemory connection");

            bool eememInitialized = await _eememMemory
                .InitializeAsync(gameClient, cancellationToken)
                .ConfigureAwait(false);
            if (!eememInitialized)
            {
                string processName = GetProcessNameForDiagnostics(gameClient);
                int processId = GetProcessIdForDiagnostics(gameClient);
                throw new InvalidOperationException(
                    $"Failed to initialize EEmemory for process '{processName}' (PID: {processId})."
                );
            }

            _doorReader = _readerFactory.CreateDoorReader(gameClient, _eememMemory);
            _enemiesReader = _readerFactory.CreateEnemiesReader(gameClient, _eememMemory);
            _inGamePlayerReader = _readerFactory.CreateInGamePlayerReader(gameClient, _eememMemory);
            _inGameScenarioReader = _readerFactory.CreateInGameScenarioReader(gameClient, _eememMemory);
            _lobbyRoomPlayerReader = _readerFactory.CreateLobbyRoomPlayerReader(gameClient, _eememMemory);
            _lobbyRoomReader = _readerFactory.CreateLobbyRoomReader(gameClient, _eememMemory);
            _lobbySlotReader = _readerFactory.CreateLobbySlotReader(gameClient, _eememMemory);

            CancellationTokenSource updateCts = new();
            CancelAndDispose(Interlocked.Exchange(ref _updateCts, updateCts));
            CancellationToken updateToken = updateCts.Token;
            Task updateLoopTask = Task.Run(() => RunUpdateLoopAsync(updateToken), CancellationToken.None);
            Task? previousUpdateLoopTask = Interlocked.Exchange(ref _updateLoopTask, updateLoopTask);
            if (previousUpdateLoopTask is not null && !previousUpdateLoopTask.IsCompleted)
            {
                _logger.LogWarning("Replacing an existing DataManager update loop during initialization.");
            }

            Volatile.Write(ref _initializeState, InitializeStateInitialized);
            _logger.LogInformation("Data manager has been initialized and wait-driven update loop started");
        }
        catch
        {
            StopUpdateLoops();
            throw;
        }
    }

    private void UpdateDoors()
    {
        _doorReader?.UpdateDoors();
        _store.DoorsState.Value = _doorReader?.DecodedDoors ?? [];
    }

    private void UpdateEnemies()
    {
        _enemiesReader?.UpdateEnemies2();
        _store.EnemiesState.Value = _enemiesReader?.DecodedEnemies2 ?? [];
    }

    private void UpdateInGamePlayer()
    {
        _inGamePlayerReader?.UpdateInGamePlayers();
        _store.InGamePlayersState.Value = _inGamePlayerReader?.DecodedInGamePlayers ?? [];
    }

    private void UpdateInGameScenario()
    {
        _inGameScenarioReader?.UpdateScenario();
        _store.InGameScenarioState.Value = _inGameScenarioReader?.DecodedScenario ?? new DecodedInGameScenario();
    }

    private void UpdateLobbyRoom()
    {
        _lobbyRoomReader?.UpdateLobbyRoom();
    }

    private void UpdateLobbyRoomPlayers()
    {
        _lobbyRoomPlayerReader?.UpdateRoomPlayers();
        _store.LobbyRoomPlayersState.Value = _lobbyRoomPlayerReader?.DecodedLobbyRoomPlayers ?? [];
    }

    private void UpdateLobbySlots()
    {
        _lobbySlotReader?.UpdateLobbySlots();
        _store.LobbySlotsState.Value = _lobbySlotReader?.DecodedLobbySlots ?? [];
    }

    private void ResetInGameData()
    {
        _logger.LogInformation("Resetting in-game data states to defaults (scenario ended or game stopped)");
        // Reset scenario status FIRST so subscribers (e.g. RunReportService) see Status=None
        // before the enemy/door/player diffs fire. Otherwise the enemy removal diff arrives
        // while _lastScenarioStatus is still InGame, causing ghost kill events for every
        // dead slot (CurHp <= 1) in the fixed 80-slot enemy array.
        _store.InGameScenarioState.Value = new DecodedInGameScenario();
        ClearLiveGameplayData();
        PublishInGameOverviewSnapshot();
    }

    private void ClearLiveGameplayData()
    {
        _store.DoorsState.Value = [];
        _store.EnemiesState.Value = [];
        _store.InGamePlayersState.Value = [];
    }

    private void PublishInGameOverviewSnapshot() =>
        _store.InGameOverviewState.Value = new InGameOverviewSnapshot(
            _store.InGameScenarioState.Value,
            _store.InGamePlayersState.Value,
            _store.EnemiesState.Value,
            _store.DoorsState.Value
        );

    private void UpdateCoreGameData()
    {
        _wasInScenario = true;
        _store.IsAtLobbyState.Value = false;
        UpdateInGameScenario();

        ScenarioStatus scenarioStatus = _store.InGameScenarioState.Value.Status;
        if (scenarioStatus.IsGameplayActive())
        {
            UpdateDoors();
            UpdateEnemies();
            UpdateInGamePlayer();
        }
        else if (!scenarioStatus.IsTransitional())
        {
            // Transition states keep the last stable gameplay snapshot alive because the
            // underlying player/enemy/item reads are temporarily unreliable.
            ClearLiveGameplayData();
        }

        PublishInGameOverviewSnapshot();
    }

    // Lobby statuses that unambiguously indicate the game has ended and the room browser is
    // active. "Launching room" and "In Game" are intentionally excluded: they persist for the
    // entire match (including in-game area-load transitions where ScenarioStatus briefly
    // becomes non-InGame) and must never trigger a premature ResetInGameData.
    private static readonly IReadOnlySet<string> _postGameLobbyStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "Waiting",
        "Hosting room",
        "Creating room",
        "Full",
    };

    private void UpdateLobbyData()
    {
        UpdateLobbyRoom();
        DecodedLobbyRoom lobbyRoom = _lobbyRoomReader?.DecodedLobbyRoom ?? new DecodedLobbyRoom();

        if (_wasInScenario)
        {
            // Defer the data reset until the lobby confirms we have truly returned to the
            // room browser. During in-game area loads LobbyRoom stays at "Launching room",
            // so the early return here prevents those transient non-InGame intervals from
            // creating spurious session splits in RunReportService.
            if (!_postGameLobbyStatuses.Contains(lobbyRoom.Status))
                return;

            _wasInScenario = false;
            ResetInGameData();
        }

        bool isAtLobby = LobbyStatusPolicy.IsActive(lobbyRoom);
        _store.IsAtLobbyState.Value = isAtLobby;
        if (!isAtLobby)
            return;

        _store.LobbyRoomState.Value = lobbyRoom;
        UpdateLobbyRoomPlayers();
        UpdateLobbySlots();
    }

    private bool IsInScenario()
    {
        IInGameScenarioReader? reader = Volatile.Read(ref _inGameScenarioReader);
        return reader is not null && reader.IsInScenario();
    }

    private async Task RunUpdateLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                RunCurrentUpdateCycle();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initial update cycle");
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                OutbreakTrackerMemoryActivityResult activity = await _memoryActivitySource
                    .WaitForActivityAsync(GetCurrentUpdateInterval(), cancellationToken)
                    .ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (!TryRunSelectiveUpdate(activity))
                    {
                        RunCurrentUpdateCycle();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during update cycle");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private TimeSpan GetCurrentUpdateInterval() => IsInScenario() ? _fastUpdateInterval : _slowUpdateInterval;

    private void RunCurrentUpdateCycle()
    {
        if (IsInScenario())
        {
            UpdateCoreGameData();
            return;
        }

        UpdateLobbyData();
    }

    private bool TryRunSelectiveUpdate(OutbreakTrackerMemoryActivityResult activity)
    {
        if (!activity.HasActivity)
        {
            return false;
        }

        if (IsInScenario())
        {
            OutbreakTrackerMemoryDomains inGameDomains = activity.Domains & InGameDomains;
            if (inGameDomains == OutbreakTrackerMemoryDomains.None)
            {
                return false;
            }

            RunSelectiveInGameUpdate(inGameDomains);
            return true;
        }

        OutbreakTrackerMemoryDomains lobbyDomains = activity.Domains & LobbyDomains;
        if (lobbyDomains == OutbreakTrackerMemoryDomains.None)
        {
            return false;
        }

        RunSelectiveLobbyUpdate(lobbyDomains);
        return true;
    }

    private void RunSelectiveInGameUpdate(OutbreakTrackerMemoryDomains domains)
    {
        _wasInScenario = true;
        _store.IsAtLobbyState.Value = false;

        ScenarioStatus previousStatus = _store.InGameScenarioState.Value.Status;
        bool scenarioUpdated = domains.HasFlag(OutbreakTrackerMemoryDomains.Scenario);
        if (scenarioUpdated)
        {
            UpdateInGameScenario();
        }

        ScenarioStatus scenarioStatus = _store.InGameScenarioState.Value.Status;
        if (!scenarioStatus.IsGameplayActive())
        {
            if (scenarioUpdated && !scenarioStatus.IsTransitional())
            {
                ClearLiveGameplayData();
            }

            PublishInGameOverviewSnapshot();
            return;
        }

        bool refreshAllGameplay = scenarioUpdated && !previousStatus.IsGameplayActive();
        if (refreshAllGameplay || domains.HasFlag(OutbreakTrackerMemoryDomains.Doors))
        {
            UpdateDoors();
        }

        if (refreshAllGameplay || domains.HasFlag(OutbreakTrackerMemoryDomains.Enemies))
        {
            UpdateEnemies();
        }

        if (refreshAllGameplay || domains.HasFlag(OutbreakTrackerMemoryDomains.InGamePlayers))
        {
            UpdateInGamePlayer();
        }

        PublishInGameOverviewSnapshot();
    }

    private void RunSelectiveLobbyUpdate(OutbreakTrackerMemoryDomains domains)
    {
        UpdateLobbyRoom();
        DecodedLobbyRoom lobbyRoom = _lobbyRoomReader?.DecodedLobbyRoom ?? new DecodedLobbyRoom();

        if (_wasInScenario)
        {
            if (!_postGameLobbyStatuses.Contains(lobbyRoom.Status))
            {
                return;
            }

            _wasInScenario = false;
            ResetInGameData();
        }

        bool isAtLobby = LobbyStatusPolicy.IsActive(lobbyRoom);
        _store.IsAtLobbyState.Value = isAtLobby;
        if (!isAtLobby)
        {
            return;
        }

        _store.LobbyRoomState.Value = lobbyRoom;

        if (domains.HasFlag(OutbreakTrackerMemoryDomains.LobbyRoomPlayers))
        {
            UpdateLobbyRoomPlayers();
        }

        if (domains.HasFlag(OutbreakTrackerMemoryDomains.LobbySlots))
        {
            UpdateLobbySlots();
        }
    }

    private void WaitForUpdateLoopShutdown(Task? updateLoopTask)
    {
        if (updateLoopTask is null)
        {
            return;
        }

        try
        {
            if (!updateLoopTask.Wait(UpdateLoopShutdownTimeout))
            {
                _logger.LogWarning(
                    "DataManager update loop did not stop within {TimeoutMilliseconds} ms.",
                    UpdateLoopShutdownTimeout.TotalMilliseconds
                );
            }
        }
        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException) { }
    }

    private void StopUpdateLoops()
    {
        Task? updateLoopTask = Interlocked.Exchange(ref _updateLoopTask, null);
        CancellationTokenSource? updateCts = Interlocked.Exchange(ref _updateCts, null);
        CancelAndDispose(updateCts);
        WaitForUpdateLoopShutdown(updateLoopTask);
        _memoryActivitySource.Detach();

        _doorReader?.Dispose();
        _doorReader = null;
        _enemiesReader?.Dispose();
        _enemiesReader = null;
        _inGamePlayerReader?.Dispose();
        _inGamePlayerReader = null;
        _inGameScenarioReader?.Dispose();
        _inGameScenarioReader = null;
        _lobbyRoomPlayerReader?.Dispose();
        _lobbyRoomPlayerReader = null;
        _lobbyRoomReader?.Dispose();
        _lobbyRoomReader = null;
        _lobbySlotReader?.Dispose();
        _lobbySlotReader = null;

        try
        {
            _store.IsAtLobbyState.Value = false;
            _wasInScenario = false;
            ResetInGameData();
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposeState) != 0)
        {
            // Ignore late stop requests racing with disposal.
        }

        Volatile.Write(ref _initializeState, InitializeStateIdle);
        _logger.LogInformation("DataManager update loops stopped; ready for re-initialization.");
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        _logger.LogInformation("Disposing DataManager...");
        _processSubscription?.Dispose();
        StopUpdateLoops();
        _loggingSubscriptions?.Dispose();

        _store.Dispose();

        _logger.LogInformation("DataManager disposed");
    }
}

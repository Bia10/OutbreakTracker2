using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.PCSX2.Client;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class GameClientConnectionServiceTests
{
    [Test]
    public async Task LaunchAndInitializeAsync_InitializesDataManagerWithLaunchedClient()
    {
        FakeGameClient launchedClient = new();
        FakeProcessLauncher launcher = new() { LaunchResult = launchedClient };
        RecordingDataManager dataManager = new();
        GameClientConnectionService service = new(
            NullLogger<GameClientConnectionService>.Instance,
            launcher,
            dataManager
        );

        IGameClient result = await service.LaunchAndInitializeAsync("pcsx2.exe", "-fastboot");

        await Assert.That(ReferenceEquals(result, launchedClient)).IsTrue();
        await Assert.That(ReferenceEquals(dataManager.LastInitializedClient, launchedClient)).IsTrue();
        await Assert.That(launcher.LaunchedFileName).IsEqualTo("pcsx2.exe");
        await Assert.That(launcher.LaunchedArguments).IsEqualTo("-fastboot");
        await Assert.That(dataManager.InitializeCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task AttachAndInitializeAsync_InitializesDataManagerWithAttachedClient()
    {
        FakeGameClient attachedClient = new();
        FakeProcessLauncher launcher = new() { AttachResult = attachedClient };
        RecordingDataManager dataManager = new();
        GameClientConnectionService service = new(
            NullLogger<GameClientConnectionService>.Instance,
            launcher,
            dataManager
        );

        IGameClient result = await service.AttachAndInitializeAsync(1234, CancellationToken.None);

        await Assert.That(ReferenceEquals(result, attachedClient)).IsTrue();
        await Assert.That(ReferenceEquals(dataManager.LastInitializedClient, attachedClient)).IsTrue();
        await Assert.That(launcher.AttachedProcessId).IsEqualTo(1234);
        await Assert.That(dataManager.InitializeCallCount).IsEqualTo(1);
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        private readonly Subject<ProcessModel> _processUpdate = new();
        private readonly Subject<bool> _isCancelling = new();
        private readonly Subject<string> _errors = new();

        public Observable<ProcessModel> ProcessUpdate => _processUpdate;

        public Observable<bool> IsCancelling => _isCancelling;

        public Process? ClientMonitoredProcess => null;

        public IGameClient? AttachedGameClient { get; private set; }

        public FakeGameClient LaunchResult { get; set; } = new();

        public FakeGameClient AttachResult { get; set; } = new();

        public string LaunchedFileName { get; private set; } = string.Empty;

        public string? LaunchedArguments { get; private set; }

        public int? AttachedProcessId { get; private set; }

        public Task<IGameClient> LaunchAsync(
            string fileName,
            string? arguments,
            CancellationToken cancellationToken = default
        )
        {
            LaunchedFileName = fileName;
            LaunchedArguments = arguments;
            AttachedGameClient = LaunchResult;
            return Task.FromResult<IGameClient>(LaunchResult);
        }

        public Task<IGameClient> AttachAsync(int processId)
        {
            AttachedProcessId = processId;
            AttachedGameClient = AttachResult;
            return Task.FromResult<IGameClient>(AttachResult);
        }

        public Task TerminateAsync(int? processId = null) => Task.CompletedTask;

        public Task KillAsync(int processId) => Task.CompletedTask;

        public Observable<string> GetErrorObservable() => _errors;

        public bool HasExited(int processId) => false;

        public int GetExitCode(int processId) => 0;

        public IGameClient? GetActiveGameClient() => AttachedGameClient;
    }

    private sealed class RecordingDataManager : IDataManager, IDisposable
    {
        private readonly ReactiveProperty<DecodedDoor[]> _doors = new([]);
        private readonly ReactiveProperty<DecodedEnemy[]> _enemies = new([]);
        private readonly ReactiveProperty<DecodedInGamePlayer[]> _players = new([]);
        private readonly ReactiveProperty<InGameOverviewSnapshot> _overview = new(new InGameOverviewSnapshot());
        private readonly ReactiveProperty<DecodedInGameScenario> _scenario = new(new DecodedInGameScenario());
        private readonly ReactiveProperty<DecodedLobbyRoom> _lobbyRoom = new(new DecodedLobbyRoom());
        private readonly ReactiveProperty<DecodedLobbyRoomPlayer[]> _lobbyPlayers = new([]);
        private readonly ReactiveProperty<DecodedLobbySlot[]> _lobbySlots = new([]);
        private readonly ReactiveProperty<bool> _isAtLobby = new(false);

        public int InitializeCallCount { get; private set; }

        public IGameClient? LastInitializedClient { get; private set; }

        public Observable<DecodedDoor[]> DoorsObservable => _doors;

        public Observable<DecodedEnemy[]> EnemiesObservable => _enemies;

        public Observable<DecodedInGamePlayer[]> InGamePlayersObservable => _players;

        public Observable<InGameOverviewSnapshot> InGameOverviewObservable => _overview;

        public Observable<DecodedInGameScenario> InGameScenarioObservable => _scenario;

        public Observable<DecodedLobbyRoom> LobbyRoomObservable => _lobbyRoom;

        public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable => _lobbyPlayers;

        public Observable<DecodedLobbySlot[]> LobbySlotsObservable => _lobbySlots;

        public Observable<bool> IsAtLobbyObservable => _isAtLobby;

        public DecodedDoor[] Doors => _doors.Value;

        public DecodedEnemy[] Enemies => _enemies.Value;

        public DecodedInGamePlayer[] InGamePlayers => _players.Value;

        public DecodedInGameScenario InGameScenario => _scenario.Value;

        public DecodedLobbyRoom LobbyRoom => _lobbyRoom.Value;

        public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _lobbyPlayers.Value;

        public DecodedLobbySlot[] LobbySlots => _lobbySlots.Value;

        public bool IsAtLobby => _isAtLobby.Value;

        public ValueTask InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken)
        {
            InitializeCallCount++;
            LastInitializedClient = gameClient;
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            _doors.Dispose();
            _enemies.Dispose();
            _players.Dispose();
            _overview.Dispose();
            _scenario.Dispose();
            _lobbyRoom.Dispose();
            _lobbyPlayers.Dispose();
            _lobbySlots.Dispose();
            _isAtLobby.Dispose();
        }
    }

    private sealed class FakeGameClient : IGameClient
    {
        public nint Handle => nint.Zero;

        public bool IsAttached => true;

        public Process? Process => null;

        public nint MainModuleBase => nint.Zero;

        public void Dispose() { }
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Readers;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class DataManagerTests
{
    [Test]
    public void Dispose_CanBeCalledMoreThanOnce()
    {
        DataManager manager = new(
            NullLogger<DataManager>.Instance,
            new FakeEEmemMemory(),
            new FakeProcessLauncher(),
            new FakeGameReaderFactory(),
            new DataManagerOptions()
        );

        try
        {
            manager.Dispose();
            manager.Dispose();
        }
        finally
        {
            manager.Dispose();
        }
    }

    [Test]
    public async Task InitializeAsync_IgnoresConcurrentSecondCall_WhileFirstInitializationIsInProgress()
    {
        BlockingEEmemMemory eememMemory = new();
        FakeGameReaderFactory readerFactory = new();
        DataManager manager = new(
            NullLogger<DataManager>.Instance,
            eememMemory,
            new FakeProcessLauncher(),
            readerFactory,
            new DataManagerOptions()
        );
        FakeGameClient gameClient = new();
        Task? firstInitializeTask = null;

        try
        {
            firstInitializeTask = manager.InitializeAsync(gameClient, CancellationToken.None).AsTask();
            await eememMemory.InitializationStarted.Task;

            Task secondInitializeTask = manager.InitializeAsync(gameClient, CancellationToken.None).AsTask();
            await secondInitializeTask;

            eememMemory.AllowInitializationToContinue.TrySetResult(true);
            await firstInitializeTask;

            await Assert.That(eememMemory.InitializeCallCount).IsEqualTo(1);
            await Assert.That(readerFactory.TotalCreateCallCount).IsEqualTo(7);
        }
        finally
        {
            eememMemory.AllowInitializationToContinue.TrySetResult(true);

            if (firstInitializeTask is not null)
                await firstInitializeTask;

            manager.Dispose();
        }
    }

    [Test]
    public async Task InitializeAsync_PublishesTransitionalScenarioStatus_WhileKeepingLastGameplayPlayers()
    {
        ScriptedInGameScenarioReader scenarioReader = new([
            CreateScenario(ScenarioStatus.InGame, frameCounter: 120),
            CreateScenario(ScenarioStatus.TransitionLoading, frameCounter: 120),
        ]);
        ScriptedInGamePlayerReader playerReader = new([
            [CreatePlayer("Kevin")],
            [],
        ]);
        ScriptedGameReaderFactory readerFactory = new(scenarioReader, playerReader);
        DataManager manager = new(
            NullLogger<DataManager>.Instance,
            new FakeEEmemMemory(),
            new FakeProcessLauncher(),
            readerFactory,
            new DataManagerOptions { FastUpdateIntervalMs = 1, SlowUpdateIntervalMs = 25 }
        );
        TaskCompletionSource<InGameOverviewSnapshot> transitionalSnapshot = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using IDisposable subscription = manager.InGameOverviewObservable.Subscribe(snapshot =>
        {
            if (snapshot.Scenario.Status == ScenarioStatus.TransitionLoading)
                transitionalSnapshot.TrySetResult(snapshot);
        });

        try
        {
            await manager.InitializeAsync(new FakeGameClient(), CancellationToken.None);

            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            InGameOverviewSnapshot snapshot = await transitionalSnapshot.Task.WaitAsync(timeout.Token);

            await Assert.That(snapshot.Players.Length).IsEqualTo(1);
            await Assert.That(snapshot.Players[0].Name).IsEqualTo("Kevin");
            await Assert.That(playerReader.UpdateCallCount).IsEqualTo(1);
        }
        finally
        {
            manager.Dispose();
        }
    }

    [Test]
    public async Task InitializeAsync_PublishesTerminalScenarioStatus_AndClearsGameplaySnapshots()
    {
        ScriptedInGameScenarioReader scenarioReader = new([
            CreateScenario(ScenarioStatus.InGame, frameCounter: 120),
            CreateScenario(ScenarioStatus.GameFinished, frameCounter: 120),
        ]);
        ScriptedInGamePlayerReader playerReader = new([
            [CreatePlayer("Kevin")],
            [CreatePlayer("Yoko")],
        ]);
        ScriptedGameReaderFactory readerFactory = new(scenarioReader, playerReader);
        DataManager manager = new(
            NullLogger<DataManager>.Instance,
            new FakeEEmemMemory(),
            new FakeProcessLauncher(),
            readerFactory,
            new DataManagerOptions { FastUpdateIntervalMs = 1, SlowUpdateIntervalMs = 25 }
        );
        TaskCompletionSource<InGameOverviewSnapshot> terminalSnapshot = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using IDisposable subscription = manager.InGameOverviewObservable.Subscribe(snapshot =>
        {
            if (snapshot.Scenario.Status == ScenarioStatus.GameFinished)
                terminalSnapshot.TrySetResult(snapshot);
        });

        try
        {
            await manager.InitializeAsync(new FakeGameClient(), CancellationToken.None);

            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            InGameOverviewSnapshot snapshot = await terminalSnapshot.Task.WaitAsync(timeout.Token);

            await Assert.That(snapshot.Scenario.Status).IsEqualTo(ScenarioStatus.GameFinished);
            await Assert.That(snapshot.Players.Length).IsEqualTo(0);
            await Assert.That(playerReader.UpdateCallCount).IsEqualTo(1);
        }
        finally
        {
            manager.Dispose();
        }
    }

    private static DecodedInGameScenario CreateScenario(ScenarioStatus status, int frameCounter) =>
        new()
        {
            CurrentFile = (byte)GameFile.FileTwo,
            ScenarioName = "Wild Things",
            FrameCounter = frameCounter,
            Status = status,
        };

    private static DecodedInGamePlayer CreatePlayer(string name) =>
        new()
        {
            Id = Ulid.NewUlid(),
            IsEnabled = true,
            IsInGame = true,
            NameId = 1,
            Name = name,
            Type = name,
        };

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        private readonly Subject<ProcessModel> _processUpdate = new();
        private readonly Subject<bool> _isCancelling = new();
        private readonly Subject<string> _errors = new();

        public Observable<ProcessModel> ProcessUpdate => _processUpdate;

        public Observable<bool> IsCancelling => _isCancelling;

        public System.Diagnostics.Process? ClientMonitoredProcess => null;

        public IGameClient? AttachedGameClient => null;

        public Task<IGameClient> LaunchAsync(
            string fileName,
            string? arguments,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<IGameClient> AttachAsync(int processId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task TerminateAsync(int? processId = null) => Task.CompletedTask;

        public Task KillAsync(int processId) => Task.CompletedTask;

        public Observable<string> GetErrorObservable() => _errors;

        public bool HasExited(int processId) => false;

        public int GetExitCode(int processId) => 0;

        public IGameClient? GetActiveGameClient() => null;
    }

    private sealed class FakeGameReaderFactory : IGameReaderFactory
    {
        public int TotalCreateCallCount { get; private set; }

        public IDoorReader CreateDoorReader(IGameClient gameClient, IEEmemAddressReader eememMemory)
        {
            TotalCreateCallCount++;
            return new FakeDoorReader();
        }

        public IEnemiesReader CreateEnemiesReader(IGameClient gameClient, IEEmemAddressReader eememMemory)
        {
            TotalCreateCallCount++;
            return new FakeEnemiesReader();
        }

        public IInGamePlayerReader CreateInGamePlayerReader(IGameClient gameClient, IEEmemAddressReader eememMemory)
        {
            TotalCreateCallCount++;
            return new FakeInGamePlayerReader();
        }

        public IInGameScenarioReader CreateInGameScenarioReader(IGameClient gameClient, IEEmemAddressReader eememMemory)
        {
            TotalCreateCallCount++;
            return new FakeInGameScenarioReader();
        }

        public ILobbyRoomPlayerReader CreateLobbyRoomPlayerReader(
            IGameClient gameClient,
            IEEmemAddressReader eememMemory
        )
        {
            TotalCreateCallCount++;
            return new FakeLobbyRoomPlayerReader();
        }

        public ILobbyRoomReader CreateLobbyRoomReader(IGameClient gameClient, IEEmemAddressReader eememMemory)
        {
            TotalCreateCallCount++;
            return new FakeLobbyRoomReader();
        }

        public ILobbySlotReader CreateLobbySlotReader(IGameClient gameClient, IEEmemAddressReader eememMemory)
        {
            TotalCreateCallCount++;
            return new FakeLobbySlotReader();
        }
    }

    private sealed class ScriptedGameReaderFactory(
        ScriptedInGameScenarioReader scenarioReader,
        ScriptedInGamePlayerReader playerReader
    ) : IGameReaderFactory
    {
        public IDoorReader CreateDoorReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            new FakeDoorReader();

        public IEnemiesReader CreateEnemiesReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            new FakeEnemiesReader();

        public IInGamePlayerReader CreateInGamePlayerReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            playerReader;

        public IInGameScenarioReader CreateInGameScenarioReader(
            IGameClient gameClient,
            IEEmemAddressReader eememMemory
        ) => scenarioReader;

        public ILobbyRoomPlayerReader CreateLobbyRoomPlayerReader(
            IGameClient gameClient,
            IEEmemAddressReader eememMemory
        ) => new FakeLobbyRoomPlayerReader();

        public ILobbyRoomReader CreateLobbyRoomReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            new FakeLobbyRoomReader();

        public ILobbySlotReader CreateLobbySlotReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            new FakeLobbySlotReader();
    }

    private sealed class FakeEEmemMemory : IEEmemMemory
    {
        public ISafeMemoryReader MemoryReader { get; } = new FakeSafeMemoryReader();

        public IStringReader StringReader { get; } = new FakeStringReader();

        public nint BaseAddress => nint.Zero;

        public ValueTask<bool> InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken) =>
            ValueTask.FromResult(true);

        public nint GetAddressFromPtr(nint ptrOffset) => nint.Zero;

        public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets) => nint.Zero;

        public bool IsAddressInBounds(nint address) => false;
    }

    private sealed class BlockingEEmemMemory : IEEmemMemory
    {
        private int _initializeCallCount;

        public TaskCompletionSource<bool> InitializationStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> AllowInitializationToContinue { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int InitializeCallCount => Volatile.Read(ref _initializeCallCount);

        public ISafeMemoryReader MemoryReader { get; } = new FakeSafeMemoryReader();

        public IStringReader StringReader { get; } = new FakeStringReader();

        public nint BaseAddress => nint.Zero;

        public async ValueTask<bool> InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _initializeCallCount);
            InitializationStarted.TrySetResult(true);
            await AllowInitializationToContinue.Task.WaitAsync(cancellationToken);
            return true;
        }

        public nint GetAddressFromPtr(nint ptrOffset) => nint.Zero;

        public nint GetAddressFromPtrChain(nint ptrOffset, params ReadOnlySpan<nint> offsets) => nint.Zero;

        public bool IsAddressInBounds(nint address) => false;
    }

    private sealed class FakeSafeMemoryReader : ISafeMemoryReader
    {
        public T Read<T>(nint hProcess, nint address)
            where T : unmanaged => default;

        public T ReadStruct<T>(nint hProcess, nint address)
            where T : struct => default;
    }

    private sealed class FakeStringReader : IStringReader
    {
        public bool TryRead(nint hProcess, nint address, out string result, System.Text.Encoding? encoding = null)
        {
            result = string.Empty;
            return true;
        }

        public string Read(nint hProcess, nint address, System.Text.Encoding? encoding = null) => string.Empty;
    }

    private sealed class FakeGameClient : IGameClient
    {
        public nint Handle => nint.Zero;

        public bool IsAttached => true;

        public System.Diagnostics.Process? Process => null;

        public nint MainModuleBase => nint.Zero;

        public void Dispose() { }
    }

    private sealed class FakeDoorReader : IDoorReader
    {
        public DecodedDoor[] DecodedDoors { get; } = [];

        public void Dispose() { }

        public void UpdateDoors() { }
    }

    private sealed class FakeEnemiesReader : IEnemiesReader
    {
        public DecodedEnemy[] DecodedEnemies2 { get; } = [];

        public void Dispose() { }

        public void UpdateEnemies2() { }
    }

    private sealed class FakeInGamePlayerReader : IInGamePlayerReader
    {
        public DecodedInGamePlayer[] DecodedInGamePlayers { get; } = [];

        public void Dispose() { }

        public void UpdateInGamePlayers() { }
    }

    private sealed class ScriptedInGamePlayerReader(DecodedInGamePlayer[][] snapshots) : IInGamePlayerReader
    {
        private readonly DecodedInGamePlayer[][] _snapshots = snapshots;
        private int _nextIndex;

        public DecodedInGamePlayer[] DecodedInGamePlayers { get; private set; } = [];

        public int UpdateCallCount { get; private set; }

        public void Dispose() { }

        public void UpdateInGamePlayers()
        {
            UpdateCallCount++;
            DecodedInGamePlayers = GetPendingSnapshot();
            AdvanceIndex();
        }

        private DecodedInGamePlayer[] GetPendingSnapshot()
        {
            int index = Math.Min(Volatile.Read(ref _nextIndex), _snapshots.Length - 1);
            return _snapshots[index];
        }

        private void AdvanceIndex()
        {
            while (true)
            {
                int current = Volatile.Read(ref _nextIndex);
                if (current >= _snapshots.Length - 1)
                    return;

                if (Interlocked.CompareExchange(ref _nextIndex, current + 1, current) == current)
                    return;
            }
        }
    }

    private sealed class FakeInGameScenarioReader : IInGameScenarioReader
    {
        public DecodedInGameScenario DecodedScenario { get; } = new();

        public void Dispose() { }

        public bool IsInScenario() => false;

        public void UpdateScenario() { }
    }

    private sealed class ScriptedInGameScenarioReader(DecodedInGameScenario[] snapshots) : IInGameScenarioReader
    {
        private readonly DecodedInGameScenario[] _snapshots = snapshots;
        private int _nextIndex;

        public DecodedInGameScenario DecodedScenario { get; private set; } = new();

        public void Dispose() { }

        public bool IsInScenario()
        {
            DecodedInGameScenario scenario = GetPendingSnapshot();
            return scenario.FrameCounter > 0 && scenario.Status is not ScenarioStatus.None and not (ScenarioStatus)0xFF;
        }

        public void UpdateScenario()
        {
            DecodedScenario = GetPendingSnapshot();
            AdvanceIndex();
        }

        private DecodedInGameScenario GetPendingSnapshot()
        {
            int index = Math.Min(Volatile.Read(ref _nextIndex), _snapshots.Length - 1);
            return _snapshots[index];
        }

        private void AdvanceIndex()
        {
            while (true)
            {
                int current = Volatile.Read(ref _nextIndex);
                if (current >= _snapshots.Length - 1)
                    return;

                if (Interlocked.CompareExchange(ref _nextIndex, current + 1, current) == current)
                    return;
            }
        }
    }

    private sealed class FakeLobbyRoomPlayerReader : ILobbyRoomPlayerReader
    {
        public DecodedLobbyRoomPlayer[] DecodedLobbyRoomPlayers { get; } = [];

        public void Dispose() { }

        public void UpdateRoomPlayers() { }
    }

    private sealed class FakeLobbyRoomReader : ILobbyRoomReader
    {
        public DecodedLobbyRoom DecodedLobbyRoom { get; } = new();

        public void Dispose() { }

        public void UpdateLobbyRoom() { }
    }

    private sealed class FakeLobbySlotReader : ILobbySlotReader
    {
        public DecodedLobbySlot[] DecodedLobbySlots { get; } = [];

        public void Dispose() { }

        public void UpdateLobbySlots() { }
    }
}

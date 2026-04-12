using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Memory.SafeMemory;
using OutbreakTracker2.Memory.String;
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
            new FakeGameReaderFactory()
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

        public Task<IGameClient> AttachAsync(int processId) => throw new NotSupportedException();

        public Task TerminateAsync(int? processId = null) => Task.CompletedTask;

        public Task KillAsync(int processId) => Task.CompletedTask;

        public Observable<string> GetErrorObservable() => _errors;

        public bool HasExited(int processId) => false;

        public int GetExitCode(int processId) => 0;

        public IGameClient? GetActiveGameClient() => null;
    }

    private sealed class FakeGameReaderFactory : IGameReaderFactory
    {
        public IDoorReader CreateDoorReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            throw new NotSupportedException();

        public IEnemiesReader CreateEnemiesReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            throw new NotSupportedException();

        public IInGamePlayerReader CreateInGamePlayerReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            throw new NotSupportedException();

        public IInGameScenarioReader CreateInGameScenarioReader(
            IGameClient gameClient,
            IEEmemAddressReader eememMemory
        ) => throw new NotSupportedException();

        public ILobbyRoomPlayerReader CreateLobbyRoomPlayerReader(
            IGameClient gameClient,
            IEEmemAddressReader eememMemory
        ) => throw new NotSupportedException();

        public ILobbyRoomReader CreateLobbyRoomReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            throw new NotSupportedException();

        public ILobbySlotReader CreateLobbySlotReader(IGameClient gameClient, IEEmemAddressReader eememMemory) =>
            throw new NotSupportedException();
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
}

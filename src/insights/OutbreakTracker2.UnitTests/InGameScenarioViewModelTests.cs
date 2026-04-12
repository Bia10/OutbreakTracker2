using Avalonia;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Views.Common;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Application.Views.GameDock;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;
using SukiUI.Toasts;

namespace OutbreakTracker2.UnitTests;

public sealed class InGameScenarioViewModelTests
{
    [Test]
    public async Task CombinedUpdateBurst_MarshalsToUiOnce_AndAppliesLatestSnapshot()
    {
        using FakeScenarioDataSource dataSource = new();
        using TestSynchronizationContextScope scope = new();
        using ScenarioEntitiesViewModel scenarioEntities = new(
            new NullToastService(),
            new StubItemImageViewModelFactory()
        );
        CountingDispatcherService dispatcher = new();
        using InGameScenarioViewModel viewModel = new(
            NullLogger<InGameScenarioViewModel>.Instance,
            dataSource,
            dispatcher,
            scenarioEntities,
            new ScenarioEntityCommands(),
            new ScenarioViewModelRouter([])
        );

        DecodedInGameScenario scenario = new()
        {
            CurrentFile = (byte)GameFile.FileTwo,
            ScenarioName = "Wild Things",
            FrameCounter = 120,
            Status = ScenarioStatus.InGame,
            Difficulty = "Normal",
            Items = new DecodedItem[GameConstants.MaxItems],
        };
        DecodedEnemy[] enemies =
        [
            new()
            {
                Id = Ulid.NewUlid(),
                Name = "Zombie",
                CurHp = 50,
                MaxHp = 50,
            },
        ];
        DecodedDoor[] doors =
        [
            new()
            {
                Id = Ulid.NewUlid(),
                SlotId = 3,
                Hp = 100,
                Flag = 1,
                Status = "Closed",
            },
        ];
        DecodedInGamePlayer[] players =
        [
            new()
            {
                Id = Ulid.NewUlid(),
                IsEnabled = true,
                IsInGame = true,
                NameId = 1,
                Name = "Kevin",
                CurHealth = 100,
                MaxHealth = 100,
                RoomId = 1,
                RoomName = "Warehouse",
                Inventory = new byte[4],
                SpecialInventory = new byte[4],
                DeadInventory = new byte[4],
                SpecialDeadInventory = new byte[4],
            },
        ];

        dataSource.PublishOverview(new InGameOverviewSnapshot(scenario, players, enemies, doors));

        await dispatcher.WaitForInvocationCountAsync(1);
        await Task.Delay(100);

        await Assert.That(dispatcher.InvocationCount).IsEqualTo(1);
        await Assert.That(viewModel.FrameCounter).IsEqualTo(120);
        await Assert.That((int)viewModel.PlayerCount).IsEqualTo(1);
        await Assert.That(viewModel.ScenarioEntitiesVm.Enemies.Count).IsEqualTo(1);
        await Assert.That(viewModel.ScenarioEntitiesVm.Doors.Count).IsEqualTo(1);
    }

    private sealed class FakeScenarioDataSource : IDataObservableSource, IDataSnapshot, IDisposable
    {
        private readonly Subject<DecodedDoor[]> _doors = new();
        private readonly Subject<DecodedEnemy[]> _enemies = new();
        private readonly Subject<DecodedInGamePlayer[]> _players = new();
        private readonly Subject<InGameOverviewSnapshot> _inGameOverview = new();
        private readonly Subject<DecodedInGameScenario> _scenario = new();
        private readonly Subject<DecodedLobbyRoom> _lobbyRoom = new();
        private readonly Subject<DecodedLobbyRoomPlayer[]> _lobbyPlayers = new();
        private readonly Subject<DecodedLobbySlot[]> _lobbySlots = new();
        private readonly Subject<bool> _isAtLobby = new();

        public DecodedDoor[] Doors { get; private set; } = [];
        public DecodedEnemy[] Enemies { get; private set; } = [];
        public DecodedInGamePlayer[] InGamePlayers { get; private set; } = [];
        public DecodedInGameScenario InGameScenario { get; private set; } = new();
        public DecodedLobbyRoom LobbyRoom { get; private set; } = new();
        public DecodedLobbyRoomPlayer[] LobbyRoomPlayers { get; private set; } = [];
        public DecodedLobbySlot[] LobbySlots { get; private set; } = [];
        public bool IsAtLobby { get; private set; }

        public Observable<DecodedDoor[]> DoorsObservable => _doors;
        public Observable<DecodedEnemy[]> EnemiesObservable => _enemies;
        public Observable<DecodedInGamePlayer[]> InGamePlayersObservable => _players;
        public Observable<InGameOverviewSnapshot> InGameOverviewObservable => _inGameOverview;
        public Observable<DecodedInGameScenario> InGameScenarioObservable => _scenario;
        public Observable<DecodedLobbyRoom> LobbyRoomObservable => _lobbyRoom;
        public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable => _lobbyPlayers;
        public Observable<DecodedLobbySlot[]> LobbySlotsObservable => _lobbySlots;
        public Observable<bool> IsAtLobbyObservable => _isAtLobby;

        public void PublishOverview(InGameOverviewSnapshot snapshot)
        {
            InGameScenario = snapshot.Scenario;
            InGamePlayers = snapshot.Players;
            Enemies = snapshot.Enemies;
            Doors = snapshot.Doors;
            _inGameOverview.OnNext(snapshot);
        }

        public void Dispose()
        {
            _doors.Dispose();
            _enemies.Dispose();
            _players.Dispose();
            _inGameOverview.Dispose();
            _scenario.Dispose();
            _lobbyRoom.Dispose();
            _lobbyPlayers.Dispose();
            _lobbySlots.Dispose();
            _isAtLobby.Dispose();
        }
    }

    private sealed class CountingDispatcherService : IDispatcherService
    {
        private readonly TaskCompletionSource _firstInvocation = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        public int InvocationCount { get; private set; }

        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action) => action();

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            action();
            _firstInvocation.TrySetResult();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            InvocationCount++;
            TResult result = action();
            _firstInvocation.TrySetResult();
            return Task.FromResult<TResult?>(result);
        }

        public async Task WaitForInvocationCountAsync(int expected)
        {
            if (InvocationCount >= expected)
                return;

            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            await _firstInvocation.Task.WaitAsync(timeout.Token);
        }
    }

    private sealed class TestSynchronizationContextScope : IDisposable
    {
        private readonly SynchronizationContext? _previousContext = SynchronizationContext.Current;

        public TestSynchronizationContextScope() =>
            SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());

        public void Dispose() => SynchronizationContext.SetSynchronizationContext(_previousContext);
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);

        public override void Send(SendOrPostCallback d, object? state) => d(state);
    }

    private sealed class NullToastService : IToastService
    {
        public Task InvokeInfoToastAsync(string content, string? title = "") => Task.CompletedTask;

        public Task InvokeSuccessToastAsync(string content, string? title = "") => Task.CompletedTask;

        public Task InvokeErrorToastAsync(string content, string? title = "") => Task.CompletedTask;

        public Task InvokeWarningToastAsync(string content, string? title = "") => Task.CompletedTask;

        public ISukiToast CreateToast(string title, object content) => throw new NotImplementedException();

        public ISukiToast CreateInfoToastWithCancelButton(
            string content,
            object cancelButtonContent,
            Action<ISukiToast> onCanceledAction,
            string? title = ""
        ) => throw new NotImplementedException();

        public Task DismissToastAsync(ISukiToast toast) => Task.CompletedTask;
    }

    private sealed class StubItemImageViewModelFactory : IItemImageViewModelFactory
    {
        public ItemImageViewModel Create() =>
            new(NullLogger<ItemImageViewModel>.Instance, new StubImageViewModelFactory());
    }

    private sealed class StubImageViewModelFactory : IImageViewModelFactory
    {
        public ImageViewModel Create() =>
            new(NullLogger<ImageViewModel>.Instance, new StubTextureAtlasService(), new ImmediateDispatcherService());
    }

    private sealed class ImmediateDispatcherService : IDispatcherService
    {
        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action) => action();

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<TResult?>(action());
    }

    private sealed class StubTextureAtlasService : ITextureAtlasService
    {
        private static readonly ITextureAtlas EmptyAtlas = new StubTextureAtlas();

        public ITextureAtlas GetAtlas(string name) => EmptyAtlas;

        public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases() => new Dictionary<string, ITextureAtlas>();

        public Task LoadAtlasesAsync() => Task.CompletedTask;
    }

    private sealed class StubTextureAtlas : ITextureAtlas
    {
        public Bitmap? Texture => null;

        public Rect GetSourceRectangle(string name) => default;
    }
}

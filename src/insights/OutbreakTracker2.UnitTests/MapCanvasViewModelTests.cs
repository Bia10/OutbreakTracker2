using Avalonia;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Views.Common;
using OutbreakTracker2.Application.Views.Common.Item;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Application.Views.Map.Canvas;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;
using SukiUI.Toasts;

namespace OutbreakTracker2.UnitTests;

public sealed class MapCanvasViewModelTests
{
    [Test]
    public async Task InGamePlayersSubscription_ResumesAfterDispatcherFailure()
    {
        using TestSynchronizationContextScope _ = new();
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new();
        using ScenarioItemsViewModel scenarioItemsViewModel = CreateScenarioItemsViewModel(dataSource);
        FlakyDispatcherService dispatcherService = new();
        using MapCanvasViewModel viewModel = new(
            dataSource,
            new PolygonCirclePackingService(),
            scenarioItemsViewModel,
            dispatcherService,
            settingsService,
            NullLogger<MapCanvasViewModel>.Instance
        );

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        dataSource.SetInGamePlayers([]);

        bool failureObserved = SpinWait.SpinUntil(
            () => dispatcherService.PostFailureCount == 1,
            TimeSpan.FromSeconds(2)
        );
        await Assert.That(failureObserved).IsTrue();

        dataSource.SetInGamePlayers([new DecodedInGamePlayer { IsEnabled = true, IsInGame = true }]);

        bool resumed = SpinWait.SpinUntil(() => viewModel.IsInGame, TimeSpan.FromSeconds(2));
        await Assert.That(resumed).IsTrue();
    }

    [Test]
    public async Task TransitionVisibilitySetting_ShowsMap_WhenEnabledAtRuntime()
    {
        using TestSynchronizationContextScope _ = new();
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new();
        using ScenarioItemsViewModel scenarioItemsViewModel = CreateScenarioItemsViewModel(dataSource);
        using MapCanvasViewModel viewModel = new(
            dataSource,
            new PolygonCirclePackingService(),
            scenarioItemsViewModel,
            new ImmediateDispatcherService(),
            settingsService,
            NullLogger<MapCanvasViewModel>.Instance
        );

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.TransitionLoading });
        dataSource.SetInGamePlayers([new DecodedInGamePlayer { IsEnabled = true, IsInGame = true }]);

        await Assert.That(viewModel.IsInGame).IsFalse();

        settingsService.SetCurrent(
            new OutbreakTrackerSettings { Display = new DisplaySettings { ShowGameplayUiDuringTransitions = true } }
        );

        bool becameVisible = SpinWait.SpinUntil(() => viewModel.IsInGame, TimeSpan.FromSeconds(2));
        await Assert.That(becameVisible).IsTrue();
    }

    [Test]
    public async Task GetProjectedScenarioItems_CollapsesProjectedStoryCopies_AfterFiltering()
    {
        using TestSynchronizationContextScope _ = new();
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new();
        using ScenarioItemsViewModel scenarioItemsViewModel = CreateScenarioItemsViewModel(dataSource);
        using MapCanvasViewModel viewModel = new(
            dataSource,
            new PolygonCirclePackingService(),
            scenarioItemsViewModel,
            new ImmediateDispatcherService(),
            settingsService,
            NullLogger<MapCanvasViewModel>.Instance
        );

        scenarioItemsViewModel.UpdateItems(
            [
                CreateScenarioItem(typeId: 10_102, typeName: "Key with a Blue Tag", roomId: 2, slotIndex: 0),
                CreateScenarioItem(typeId: 10_102, typeName: "Key with a Blue Tag", roomId: 2, slotIndex: 4),
            ],
            static item => item,
            frameCounter: 0,
            GameFile.FileOne
        );

        scenarioItemsViewModel.Items[1].ToggleMapProjectionCommand.Execute(null);

        IReadOnlyList<ScenarioItemSlotViewModel> projectedItems = viewModel.GetProjectedScenarioItems();

        await Assert.That(projectedItems.Count).IsEqualTo(1);
        await Assert.That(ReferenceEquals(projectedItems[0], scenarioItemsViewModel.Items[1])).IsTrue();
    }

    [Test]
    public async Task GetProjectedScenarioItems_CollapsesMirroredStoryCopies_WhenProjectAllIsEnabled()
    {
        using TestSynchronizationContextScope _ = new();
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    ScenarioItemsDock = new ScenarioItemsDockSettings { ProjectAllOntoMap = true },
                },
            }
        );
        using ScenarioItemsViewModel scenarioItemsViewModel = CreateScenarioItemsViewModel(dataSource);
        using MapCanvasViewModel viewModel = new(
            dataSource,
            new PolygonCirclePackingService(),
            scenarioItemsViewModel,
            new ImmediateDispatcherService(),
            settingsService,
            NullLogger<MapCanvasViewModel>.Instance
        );

        scenarioItemsViewModel.UpdateItems(
            [
                CreateScenarioItem(typeId: 11_000, typeName: "Employee Area Key", roomId: 30, slotIndex: 0),
                CreateScenarioItem(typeId: 11_000, typeName: "Employee Area Key", roomId: 30, slotIndex: 2),
                CreateScenarioItem(typeId: 11_000, typeName: "Employee Area Key", roomId: 30, slotIndex: 3),
                CreateScenarioItem(typeId: 11_000, typeName: "Employee Area Key", roomId: 30, slotIndex: 4),
            ],
            static item => item,
            frameCounter: 0,
            GameFile.FileOne
        );

        IReadOnlyList<ScenarioItemSlotViewModel> projectedItems = viewModel.GetProjectedScenarioItems();

        await Assert.That(projectedItems.Count).IsEqualTo(1);
        await Assert.That(projectedItems[0].SlotIndex).IsEqualTo((byte)2);
    }

    private static DecodedItem CreateScenarioItem(short typeId, string typeName, byte roomId, byte slotIndex) =>
        new()
        {
            Id = typeId,
            TypeId = typeId,
            TypeName = typeName,
            Quantity = 1,
            Present = 1,
            RoomId = roomId,
            RoomName = $"Room {roomId}",
            SlotIndex = slotIndex,
        };

    private static ScenarioItemsViewModel CreateScenarioItemsViewModel(IDataObservableSource dataSource) =>
        new(
            NullLogger<ScenarioItemsViewModel>.Instance,
            dataSource,
            new ImmediateDispatcherService(),
            new NullToastService(),
            new StubItemImageViewModelFactory()
        );

    private sealed class FakeDataSource : IDataObservableSource, IDisposable
    {
        private readonly ReactiveProperty<DecodedDoor[]> _doors = new([]);
        private readonly ReactiveProperty<DecodedEnemy[]> _enemies = new([]);
        private readonly ReactiveProperty<DecodedInGamePlayer[]> _inGamePlayers = new([]);
        private readonly ReactiveProperty<InGameOverviewSnapshot> _inGameOverview = new(new InGameOverviewSnapshot());
        private readonly ReactiveProperty<DecodedInGameScenario> _inGameScenario = new(new DecodedInGameScenario());
        private readonly ReactiveProperty<DecodedLobbyRoom> _lobbyRoom = new(new DecodedLobbyRoom());
        private readonly ReactiveProperty<DecodedLobbyRoomPlayer[]> _lobbyRoomPlayers = new([]);
        private readonly ReactiveProperty<DecodedLobbySlot[]> _lobbySlots = new([]);
        private readonly ReactiveProperty<bool> _isAtLobby = new(false);

        public Observable<DecodedDoor[]> DoorsObservable => _doors;

        public Observable<DecodedEnemy[]> EnemiesObservable => _enemies;

        public Observable<DecodedInGamePlayer[]> InGamePlayersObservable => _inGamePlayers;

        public Observable<InGameOverviewSnapshot> InGameOverviewObservable => _inGameOverview;

        public Observable<DecodedInGameScenario> InGameScenarioObservable => _inGameScenario;

        public Observable<DecodedLobbyRoom> LobbyRoomObservable => _lobbyRoom;

        public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable => _lobbyRoomPlayers;

        public Observable<DecodedLobbySlot[]> LobbySlotsObservable => _lobbySlots;

        public Observable<bool> IsAtLobbyObservable => _isAtLobby;

        public void SetInGamePlayers(DecodedInGamePlayer[] players) => _inGamePlayers.Value = players;

        public void SetScenario(DecodedInGameScenario scenario) => _inGameScenario.Value = scenario;

        public void Dispose()
        {
            _doors.Dispose();
            _enemies.Dispose();
            _inGamePlayers.Dispose();
            _inGameOverview.Dispose();
            _inGameScenario.Dispose();
            _lobbyRoom.Dispose();
            _lobbyRoomPlayers.Dispose();
            _lobbySlots.Dispose();
            _isAtLobby.Dispose();
        }
    }

    private sealed class FakeAppSettingsService : IAppSettingsService
    {
        private readonly ReactiveProperty<OutbreakTrackerSettings> _settings;

        public FakeAppSettingsService(OutbreakTrackerSettings? settings = null)
        {
            _settings = new ReactiveProperty<OutbreakTrackerSettings>(settings ?? new OutbreakTrackerSettings());
        }

        public string UserSettingsPath { get; } =
            Path.Combine(Path.GetTempPath(), "outbreaktracker2-test-settings.json");

        public OutbreakTrackerSettings Current => _settings.Value;

        public Observable<OutbreakTrackerSettings> SettingsObservable => _settings;

        public void SetCurrent(OutbreakTrackerSettings settings) => _settings.Value = settings;

        public ValueTask SaveAsync(OutbreakTrackerSettings settings, CancellationToken cancellationToken = default)
        {
            _settings.Value = settings;
            return ValueTask.CompletedTask;
        }

        public ValueTask ExportAsync(Stream destination, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask<OutbreakTrackerSettings> ImportAsync(
            Stream source,
            CancellationToken cancellationToken = default
        ) => ValueTask.FromResult(Current);

        public ValueTask<OutbreakTrackerSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Current);

        public void Dispose() => _settings.Dispose();
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
        public Task InvokeSuccessToastAsync(string text, string? title = null) => Task.CompletedTask;

        public Task InvokeInfoToastAsync(string text, string? title = null) => Task.CompletedTask;

        public Task InvokeWarningToastAsync(string text, string? title = null) => Task.CompletedTask;

        public Task InvokeErrorToastAsync(string text, string? title = null) => Task.CompletedTask;

        public ISukiToast CreateToast(string title, object content)
        {
            throw new NotImplementedException();
        }

        public ISukiToast CreateInfoToastWithCancelButton(
            string content,
            object cancelButtonContent,
            Action<ISukiToast> onCanceledAction,
            string? title = ""
        )
        {
            throw new NotImplementedException();
        }

        public Task DismissToastAsync(ISukiToast toast)
        {
            throw new NotImplementedException();
        }
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

        public bool TryGetSourceRectangle(string name, out Rect rect)
        {
            rect = default;
            return false;
        }

        public Rect GetSourceRectangle(string name) => default;
    }

    private sealed class ImmediateDispatcherService : IDispatcherService
    {
        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action) => action();

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TResult?>(action());
        }
    }

    private sealed class FlakyDispatcherService : IDispatcherService
    {
        private int _remainingPostFailures = 1;

        public int PostFailureCount { get; private set; }

        public bool CheckAccess() => true;

        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action)
        {
            if (Interlocked.Decrement(ref _remainingPostFailures) >= 0)
            {
                PostFailureCount++;
                throw new InvalidOperationException("Simulated dispatcher failure.");
            }

            action();
        }

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TResult?>(action());
        }
    }
}

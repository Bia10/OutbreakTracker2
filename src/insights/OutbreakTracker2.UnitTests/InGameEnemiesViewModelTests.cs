using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class InGameEnemiesViewModelTests
{
    [Test]
    public async Task Dispose_ClearsTrackedEnemiesBeforeReturning_WhenNotOnUiThread()
    {
        using TestSynchronizationContextScope scope = new();
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new();
        using FakeTrackerRegistry trackerRegistry = new();
        QueuedPostDispatcherService dispatcher = new();
        using InGameEnemiesViewModel viewModel = new(
            NullLogger<InGameEnemiesViewModel>.Instance,
            dispatcher,
            dataSource,
            TimeProvider.System,
            settingsService,
            trackerRegistry
        );

        dataSource.SetScenario(
            new DecodedInGameScenario { ScenarioName = "Wild Things", Status = ScenarioStatus.InGame }
        );
        trackerRegistry.EmitEnemyDiff(new CollectionDiff<DecodedEnemy>([CreateEnemy()], [], []));

        await Assert.That(viewModel.EnemiesView.Count).IsEqualTo(1);
        await Assert.That(viewModel.HasEnemies).IsTrue();

        viewModel.Dispose();

        await Assert.That(viewModel.EnemiesView.Count).IsEqualTo(0);
        await Assert.That(viewModel.HasEnemies).IsFalse();
    }

    [Test]
    public async Task DisposeAsync_ClearsTrackedEnemiesBeforeReturning_WhenNotOnUiThread()
    {
        using TestSynchronizationContextScope scope = new();
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new();
        using FakeTrackerRegistry trackerRegistry = new();
        QueuedPostDispatcherService dispatcher = new();
        await using InGameEnemiesViewModel viewModel = new(
            NullLogger<InGameEnemiesViewModel>.Instance,
            dispatcher,
            dataSource,
            TimeProvider.System,
            settingsService,
            trackerRegistry
        );

        dataSource.SetScenario(
            new DecodedInGameScenario { ScenarioName = "Wild Things", Status = ScenarioStatus.InGame }
        );
        trackerRegistry.EmitEnemyDiff(new CollectionDiff<DecodedEnemy>([CreateEnemy()], [], []));

        await Assert.That(viewModel.EnemiesView.Count).IsEqualTo(1);
        await Assert.That(viewModel.HasEnemies).IsTrue();

        await viewModel.DisposeAsync();

        await Assert.That(viewModel.EnemiesView.Count).IsEqualTo(0);
        await Assert.That(viewModel.HasEnemies).IsFalse();
    }

    [Test]
    public async Task MixedEnemyDiff_ReconcilesExistingAndNewCards_WithoutDuplicateSpecificLogic()
    {
        using TestSynchronizationContextScope scope = new();
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new();
        using FakeTrackerRegistry trackerRegistry = new();
        QueuedPostDispatcherService dispatcher = new();
        using InGameEnemiesViewModel viewModel = new(
            NullLogger<InGameEnemiesViewModel>.Instance,
            dispatcher,
            dataSource,
            TimeProvider.System,
            settingsService,
            trackerRegistry
        );

        DecodedEnemy firstEnemy = CreateEnemy();
        DecodedEnemy secondEnemy = CreateEnemy() with { Id = Ulid.NewUlid(), SlotId = 2, Name = "Zombie" };
        DecodedEnemy updatedFirstEnemy = firstEnemy with { CurHp = 1200 };
        DecodedEnemy thirdEnemy = CreateEnemy() with { Id = Ulid.NewUlid(), SlotId = 3, Name = "Licker" };

        dataSource.SetScenario(
            new DecodedInGameScenario { ScenarioName = "Wild Things", Status = ScenarioStatus.InGame }
        );
        trackerRegistry.EmitEnemyDiff(new CollectionDiff<DecodedEnemy>([firstEnemy, secondEnemy], [], []));

        await Assert.That(viewModel.EnemiesView.Count).IsEqualTo(2);
        await Assert.That(viewModel.EnemiesView[0].UniqueId).IsEqualTo(firstEnemy.Id);
        await Assert.That(viewModel.EnemiesView[1].UniqueId).IsEqualTo(secondEnemy.Id);

        trackerRegistry.EmitEnemyDiff(
            new CollectionDiff<DecodedEnemy>(
                [thirdEnemy],
                [secondEnemy],
                [new EntityChange<DecodedEnemy>(firstEnemy, updatedFirstEnemy)]
            )
        );

        await Assert.That(viewModel.EnemiesView.Count).IsEqualTo(2);
        await Assert.That(viewModel.EnemiesView[0].UniqueId).IsEqualTo(firstEnemy.Id);
        await Assert.That(viewModel.EnemiesView[0].CurrentHp).IsEqualTo((ushort)1200);
        await Assert.That(viewModel.EnemiesView[1].UniqueId).IsEqualTo(thirdEnemy.Id);
    }

    private static DecodedEnemy CreateEnemy() =>
        new()
        {
            Id = Ulid.NewUlid(),
            Enabled = 1,
            InGame = 1,
            SlotId = 1,
            RoomId = 2,
            NameId = 49,
            Name = "Hunter",
            CurHp = 1550,
            MaxHp = 1550,
        };

    private sealed class FakeTrackerRegistry : ITrackerRegistry, IDisposable
    {
        private readonly Subject<AlertNotification> _alerts = new();

        public FakeEntityChangeSource<DecodedEnemy> EnemyChangesSource { get; } = new();

        public IReadOnlyEntityTracker<DecodedEnemy> Enemies => null!;

        public IReadOnlyEntityTracker<DecodedDoor> Doors => null!;

        public IReadOnlyEntityTracker<DecodedInGamePlayer> Players => null!;

        public IReadOnlyEntityTracker<DecodedLobbySlot> LobbySlots => null!;

        public IEntityChangeSource<DecodedEnemy> EnemyChanges => EnemyChangesSource;

        public IEntityChangeSource<DecodedDoor> DoorChanges => null!;

        public IEntityChangeSource<DecodedInGamePlayer> PlayerChanges => null!;

        public IEntityChangeSource<DecodedLobbySlot> LobbySlotChanges => null!;

        public Observable<AlertNotification> AllAlerts => _alerts;

        public void EmitEnemyDiff(CollectionDiff<DecodedEnemy> diff) => EnemyChangesSource.EmitDiff(diff);

        public void Dispose()
        {
            EnemyChangesSource.Dispose();
            _alerts.Dispose();
        }
    }

    private sealed class FakeEntityChangeSource<T> : IEntityChangeSource<T>
        where T : IHasId
    {
        private readonly Subject<T> _added = new();
        private readonly Subject<T> _removed = new();
        private readonly Subject<EntityChange<T>> _updated = new();
        private readonly Subject<CollectionDiff<T>> _diffs = new();

        public Observable<T> Added => _added;

        public Observable<T> Removed => _removed;

        public Observable<EntityChange<T>> Updated => _updated;

        public Observable<CollectionDiff<T>> Diffs => _diffs;

        public void EmitDiff(CollectionDiff<T> diff) => _diffs.OnNext(diff);

        public void Dispose()
        {
            _added.Dispose();
            _removed.Dispose();
            _updated.Dispose();
            _diffs.Dispose();
        }
    }

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
        private readonly ReactiveProperty<OutbreakTrackerSettings> _settings = new(
            new OutbreakTrackerSettings { Display = new DisplaySettings { ShowGameplayUiDuringTransitions = true } }
        );

        public string UserSettingsPath => string.Empty;

        public OutbreakTrackerSettings Current => _settings.Value;

        public Observable<OutbreakTrackerSettings> SettingsObservable => _settings;

        public ValueTask SaveAsync(OutbreakTrackerSettings settings, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

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

    private sealed class QueuedPostDispatcherService : IDispatcherService
    {
        private readonly List<Action> _postedActions = [];

        public bool IsOnUIThread() => false;

        public void PostOnUI(Action action) => _postedActions.Add(action);

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
}

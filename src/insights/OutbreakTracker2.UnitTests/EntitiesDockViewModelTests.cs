using System.Collections.ObjectModel;
using System.Collections.Specialized;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Application.Views.GameDock.Dockables;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class EntitiesDockViewModelTests
{
    [Test]
    public async Task FiltersEnemiesToCurrentPlayerRoom_WhenSettingIsEnabled()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    EntitiesDock = new EntitiesDockSettings { OnlyShowCurrentPlayerRoom = true },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();

        FakeEnemyCardCollectionSource source = new();
        source.Add(CreateEnemyViewModel(roomId: 1, slotId: 1, currentHp: 1550));
        source.Add(CreateEnemyViewModel(roomId: 3, slotId: 2, currentHp: 1400));

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1), CreatePlayer(slotIndex: 1, roomId: 3)]);

        using EntitiesDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        List<InGameEnemyViewModel> visibleEnemies = ReadVisibleEnemies(viewModel);

        await Assert.That(viewModel.HasEnemies).IsTrue();
        await Assert.That(visibleEnemies.Count).IsEqualTo(1);
        await Assert.That(visibleEnemies[0].RoomId).IsEqualTo((byte)1);
        await Assert.That(visibleEnemies[0].HealthStatus).IsEqualTo("1550");
    }

    [Test]
    public async Task RefiltersEnemies_WhenCurrentPlayerRoomChanges()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    EntitiesDock = new EntitiesDockSettings { OnlyShowCurrentPlayerRoom = true },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();

        FakeEnemyCardCollectionSource source = new();
        source.Add(CreateEnemyViewModel(roomId: 1, slotId: 1, currentHp: 1550));
        source.Add(CreateEnemyViewModel(roomId: 3, slotId: 2, currentHp: 1400));

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1)]);

        using EntitiesDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 3)]);

        List<InGameEnemyViewModel> visibleEnemies = ReadVisibleEnemies(viewModel);

        await Assert.That(visibleEnemies.Count).IsEqualTo(1);
        await Assert.That(visibleEnemies[0].RoomId).IsEqualTo((byte)3);
    }

    [Test]
    public async Task ShowsAllEnemies_WhenSettingIsDisabledAtRuntime()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    EntitiesDock = new EntitiesDockSettings { OnlyShowCurrentPlayerRoom = true },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();

        FakeEnemyCardCollectionSource source = new();
        source.Add(CreateEnemyViewModel(roomId: 1, slotId: 1, currentHp: 1550));
        source.Add(CreateEnemyViewModel(roomId: 3, slotId: 2, currentHp: 1400));

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1)]);

        using EntitiesDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        settingsService.SetCurrent(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    EntitiesDock = new EntitiesDockSettings { OnlyShowCurrentPlayerRoom = false },
                },
            }
        );

        List<InGameEnemyViewModel> visibleEnemies = ReadVisibleEnemies(viewModel);

        await Assert.That(visibleEnemies.Count).IsEqualTo(2);
    }

    [Test]
    public async Task FiltersEnemies_WhenLocalPlayerSlotIsUnavailableButAllTrackedPlayersShareARoom()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    EntitiesDock = new EntitiesDockSettings { OnlyShowCurrentPlayerRoom = true },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();

        FakeEnemyCardCollectionSource source = new();
        source.Add(CreateEnemyViewModel(roomId: 1, slotId: 1, currentHp: 1550));
        source.Add(CreateEnemyViewModel(roomId: 3, slotId: 2, currentHp: 1400));

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = byte.MaxValue });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 3), CreatePlayer(slotIndex: 1, roomId: 3)]);

        using EntitiesDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        List<InGameEnemyViewModel> visibleEnemies = ReadVisibleEnemies(viewModel);

        await Assert.That(visibleEnemies.Count).IsEqualTo(1);
        await Assert.That(visibleEnemies[0].RoomId).IsEqualTo((byte)3);
    }

    [Test]
    public async Task DoesNotResetVisibleEnemies_WhenCurrentPlayerRoomDoesNotChange()
    {
        using FakeDataSource dataSource = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                Display = new DisplaySettings
                {
                    EntitiesDock = new EntitiesDockSettings { OnlyShowCurrentPlayerRoom = true },
                },
            }
        );
        using TestSynchronizationContextScope scope = new();

        FakeEnemyCardCollectionSource source = new();
        source.Add(CreateEnemyViewModel(roomId: 1, slotId: 1, currentHp: 1550));
        source.Add(CreateEnemyViewModel(roomId: 3, slotId: 2, currentHp: 1400));

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1)]);

        using EntitiesDockViewModel viewModel = new(
            source,
            dataSource,
            dataSource,
            settingsService,
            new ImmediateDispatcherService()
        );

        int collectionChangedCount = 0;
        ((INotifyCollectionChanged)viewModel.EnemiesView).CollectionChanged += (_, _) => collectionChangedCount++;

        dataSource.SetScenario(new DecodedInGameScenario { LocalPlayerSlotIndex = 0 });
        dataSource.SetInGamePlayers([CreatePlayer(slotIndex: 0, roomId: 1)]);

        await Assert.That(collectionChangedCount).IsEqualTo(0);
    }

    private static InGameEnemyViewModel CreateEnemyViewModel(byte roomId, short slotId, ushort currentHp)
    {
        DecodedEnemy enemy = new()
        {
            Id = Ulid.NewUlid(),
            Enabled = 1,
            InGame = 1,
            SlotId = slotId,
            RoomId = roomId,
            NameId = 49,
            Name = "Hunter",
            CurHp = currentHp,
            MaxHp = 1550,
            BossType = 0,
            Status = 0,
        };

        return new InGameEnemyViewModel(enemy, "Unknown");
    }

    private static DecodedInGamePlayer CreatePlayer(int slotIndex, short roomId) =>
        new()
        {
            Id = Ulid.NewUlid(),
            IsEnabled = true,
            IsInGame = true,
            SlotIndex = slotIndex,
            NameId = 1,
            Name = "Kevin",
            Type = "Kevin",
            RoomId = roomId,
        };

    private static List<InGameEnemyViewModel> ReadVisibleEnemies(EntitiesDockViewModel viewModel)
    {
        List<InGameEnemyViewModel> visibleEnemies = [];

        foreach (InGameEnemyViewModel enemy in viewModel.EnemiesView)
            visibleEnemies.Add(enemy);

        return visibleEnemies;
    }

    private sealed class FakeEnemyCardCollectionSource : IEnemyCardCollectionSource
    {
        private readonly ObservableCollection<InGameEnemyViewModel> _enemies = [];

        public IEnumerable<InGameEnemyViewModel> Enemies => _enemies;

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => _enemies.CollectionChanged += value;
            remove => _enemies.CollectionChanged -= value;
        }

        public void Add(InGameEnemyViewModel enemy) => _enemies.Add(enemy);
    }

    private sealed class FakeDataSource : IDataObservableSource, IDataSnapshot, IDisposable
    {
        private readonly ReactiveProperty<DecodedDoor[]> _doors = new([]);
        private readonly ReactiveProperty<DecodedEnemy[]> _enemies = new([]);
        private readonly ReactiveProperty<DecodedInGamePlayer[]> _inGamePlayers = new([]);
        private readonly ReactiveProperty<DecodedInGameScenario> _inGameScenario = new(new DecodedInGameScenario());
        private readonly ReactiveProperty<DecodedLobbyRoom> _lobbyRoom = new(new DecodedLobbyRoom());
        private readonly ReactiveProperty<DecodedLobbyRoomPlayer[]> _lobbyRoomPlayers = new([]);
        private readonly ReactiveProperty<DecodedLobbySlot[]> _lobbySlots = new([]);
        private readonly ReactiveProperty<bool> _isAtLobby = new(false);

        public DecodedDoor[] Doors => _doors.Value;
        public DecodedEnemy[] Enemies => _enemies.Value;
        public DecodedInGamePlayer[] InGamePlayers => _inGamePlayers.Value;
        public DecodedInGameScenario InGameScenario => _inGameScenario.Value;
        public DecodedLobbyRoom LobbyRoom => _lobbyRoom.Value;
        public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _lobbyRoomPlayers.Value;
        public DecodedLobbySlot[] LobbySlots => _lobbySlots.Value;
        public bool IsAtLobby => _isAtLobby.Value;

        public Observable<DecodedDoor[]> DoorsObservable => _doors;
        public Observable<DecodedEnemy[]> EnemiesObservable => _enemies;
        public Observable<DecodedInGamePlayer[]> InGamePlayersObservable => _inGamePlayers;
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

        public FakeAppSettingsService(OutbreakTrackerSettings settings)
        {
            _settings = new ReactiveProperty<OutbreakTrackerSettings>(settings);
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
        )
        {
            TResult result = action();
            return Task.FromResult<TResult?>(result);
        }
    }
}

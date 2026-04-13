using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.PCSX2.Client;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class TrackerRegistryLobbyAlertTests
{
    [Test]
    public async Task LobbyGameCreatedAlert_IsEmitted_WhenSlotBecomesAnActiveGameAtLobby()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new();
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        Ulid slotId = Ulid.NewUlid();
        dataManager.SetLobbyPresence(true);
        dataManager.SetLobbySlots([CreateInactiveSlot(slotId)]);
        dataManager.SetLobbySlots([CreateActiveSlot(slotId, 4, "Training Room", "Training Ground")]);

        await Assert.That(alerts.Count).IsEqualTo(1);
        await Assert.That(alerts[0].Title).IsEqualTo("Lobby Game Created");
        await Assert.That(alerts[0].Message.Contains("Training Room", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task LobbyGameCreatedAlert_IsSuppressed_WhenNotAtLobby()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new();
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        Ulid slotId = Ulid.NewUlid();
        dataManager.SetLobbyPresence(false);
        dataManager.SetLobbySlots([CreateInactiveSlot(slotId)]);
        dataManager.SetLobbySlots([CreateActiveSlot(slotId, 7, "Night Raid", "Wild Things")]);

        await Assert.That(alerts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task LobbyRoutineSlotChanges_DoNotEmitAlerts()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new();
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        Ulid slotId = Ulid.NewUlid();
        dataManager.SetLobbyPresence(true);
        dataManager.SetLobbySlots([CreateInactiveSlot(slotId)]);
        dataManager.SetLobbySlots([CreateActiveSlot(slotId, 2, "Downtown", "Outbreak")]);

        alerts.Clear();

        dataManager.SetLobbySlots([
            CreateActiveSlot(slotId, 2, "Downtown", "Outbreak") with
            {
                CurPlayers = 4,
                MaxPlayers = 4,
                Status = "Full",
                IsPassProtected = true,
            },
        ]);

        await Assert.That(alerts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task LobbyGameCreatedAlert_IsSuppressed_WhenRuleIsDisabledInSettings()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new();
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        settingsService.SetCurrent(
            settingsService.Current with
            {
                AlertRules = settingsService.Current.AlertRules with
                {
                    Lobby = settingsService.Current.AlertRules.Lobby with { GameCreated = false },
                },
            }
        );

        Ulid slotId = Ulid.NewUlid();
        dataManager.SetLobbyPresence(true);
        dataManager.SetLobbySlots([CreateInactiveSlot(slotId)]);
        dataManager.SetLobbySlots([CreateActiveSlot(slotId, 1, "City Center", "Below Freezing Point")]);

        await Assert.That(alerts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task LobbyNameMatchAlert_IsEmitted_WhenCreatedLobbyTitleMatchesConfiguredFilter()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new();
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        settingsService.SetCurrent(
            settingsService.Current with
            {
                AlertRules = settingsService.Current.AlertRules with
                {
                    Lobby = settingsService.Current.AlertRules.Lobby with
                    {
                        GameCreated = false,
                        NameMatchCreated = true,
                        NameMatchFilter = "Training",
                    },
                },
            }
        );

        Ulid slotId = Ulid.NewUlid();
        dataManager.SetLobbyPresence(true);
        dataManager.SetLobbySlots([CreateInactiveSlot(slotId)]);
        dataManager.SetLobbySlots([CreateActiveSlot(slotId, 3, "Training Room", "Showdown 3")]);

        await Assert.That(alerts.Count).IsEqualTo(1);
        await Assert.That(alerts[0].Title).IsEqualTo("Tracked Lobby Name Created");
    }

    [Test]
    public async Task LobbyScenarioMatchAlert_IsEmitted_WhenCreatedLobbyScenarioMatchesConfiguredFilter()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new();
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        settingsService.SetCurrent(
            settingsService.Current with
            {
                AlertRules = settingsService.Current.AlertRules with
                {
                    Lobby = settingsService.Current.AlertRules.Lobby with
                    {
                        GameCreated = false,
                        ScenarioMatchCreated = true,
                        ScenarioMatchFilter = "Wild things",
                    },
                },
            }
        );

        Ulid slotId = Ulid.NewUlid();
        dataManager.SetLobbyPresence(true);
        dataManager.SetLobbySlots([CreateInactiveSlot(slotId)]);
        dataManager.SetLobbySlots([CreateActiveSlot(slotId, 5, "Night Raid", "Wild Things")]);

        await Assert.That(alerts.Count).IsEqualTo(1);
        await Assert.That(alerts[0].Title).IsEqualTo("Tracked Lobby Scenario Created");
    }

    [Test]
    public async Task LobbyScenarioMatchAlert_IsEmitted_ForLegacyPartialScenarioFilter()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new();
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        settingsService.SetCurrent(
            settingsService.Current with
            {
                AlertRules = settingsService.Current.AlertRules with
                {
                    Lobby = settingsService.Current.AlertRules.Lobby with
                    {
                        GameCreated = false,
                        ScenarioMatchCreated = true,
                        ScenarioMatchFilter = "Wild",
                    },
                },
            }
        );

        Ulid slotId = Ulid.NewUlid();
        dataManager.SetLobbyPresence(true);
        dataManager.SetLobbySlots([CreateInactiveSlot(slotId)]);
        dataManager.SetLobbySlots([CreateActiveSlot(slotId, 6, "Night Raid", "Wild things")]);

        await Assert.That(alerts.Count).IsEqualTo(1);
        await Assert.That(alerts[0].Title).IsEqualTo("Tracked Lobby Scenario Created");
    }

    [Test]
    public async Task LobbyNameMatchAlert_IsSuppressed_WhenCreatedLobbyTitleDoesNotMatchConfiguredFilter()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new();
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);
        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        settingsService.SetCurrent(
            settingsService.Current with
            {
                AlertRules = settingsService.Current.AlertRules with
                {
                    Lobby = settingsService.Current.AlertRules.Lobby with
                    {
                        GameCreated = false,
                        NameMatchCreated = true,
                        NameMatchFilter = "Training",
                    },
                },
            }
        );

        Ulid slotId = Ulid.NewUlid();
        dataManager.SetLobbyPresence(true);
        dataManager.SetLobbySlots([CreateInactiveSlot(slotId)]);
        dataManager.SetLobbySlots([CreateActiveSlot(slotId, 2, "Downtown", "Outbreak")]);

        await Assert.That(alerts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task VirusWarningAlert_UsesConfiguredThreshold()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                AlertRules = new AlertRuleSettings
                {
                    Players = new PlayerAlertRuleSettings
                    {
                        VirusWarningThreshold = 60.0,
                        VirusCriticalThreshold = 90.0,
                    },
                },
            }
        );
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        Ulid playerId = Ulid.NewUlid();
        dataManager.SetInGamePlayers([CreatePlayer(playerId, 59.0)]);
        dataManager.SetInGamePlayers([CreatePlayer(playerId, 61.0)]);

        await Assert.That(alerts.Count).IsEqualTo(1);
        await Assert.That(alerts[0].Title).IsEqualTo("Virus Warning");
    }

    [Test]
    public async Task VirusWarningAlert_IsEmitted_WhenPlayerFirstAppearsAboveThreshold()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                AlertRules = new AlertRuleSettings
                {
                    Players = new PlayerAlertRuleSettings
                    {
                        VirusWarningEnabled = true,
                        VirusWarningThreshold = 60.0,
                        VirusCriticalEnabled = false,
                    },
                },
            }
        );
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        Ulid playerId = Ulid.NewUlid();
        dataManager.SetInGamePlayers([CreatePlayer(playerId, 61.0)]);

        await Assert.That(alerts.Count).IsEqualTo(1);
        await Assert.That(alerts[0].Title).IsEqualTo("Virus Warning");
    }

    [Test]
    public async Task VirusCriticalAlert_IsEmitted_WhenPlayerFirstAppearsAboveThreshold()
    {
        using FakeDataManager dataManager = new();
        using FakeAppSettingsService settingsService = new(
            new OutbreakTrackerSettings
            {
                AlertRules = new AlertRuleSettings
                {
                    Players = new PlayerAlertRuleSettings
                    {
                        VirusWarningEnabled = false,
                        VirusCriticalEnabled = true,
                        VirusCriticalThreshold = 90.0,
                    },
                },
            }
        );
        using TrackerRegistry trackerRegistry = CreateTrackerRegistry(dataManager, settingsService);

        List<AlertNotification> alerts = [];
        using IDisposable subscription = trackerRegistry.AllAlerts.Subscribe(alert => alerts.Add(alert));

        Ulid playerId = Ulid.NewUlid();
        dataManager.SetInGamePlayers([CreatePlayer(playerId, 91.0)]);

        await Assert.That(alerts.Count).IsEqualTo(1);
        await Assert.That(alerts[0].Title).IsEqualTo("Virus Critical");
    }

    private static DecodedLobbySlot CreateInactiveSlot(Ulid slotId) =>
        new()
        {
            Id = slotId,
            SlotNumber = -1,
            CurPlayers = -1,
            MaxPlayers = -1,
            Status = "Unknown",
        };

    private static DecodedLobbySlot CreateActiveSlot(Ulid slotId, short slotNumber, string title, string scenarioId) =>
        new()
        {
            Id = slotId,
            SlotNumber = slotNumber,
            CurPlayers = 1,
            MaxPlayers = 4,
            Status = "Vacant",
            Title = title,
            ScenarioId = scenarioId,
            Version = "File 2",
        };

    private static TrackerRegistry CreateTrackerRegistry(
        FakeDataManager dataManager,
        FakeAppSettingsService settingsService
    ) =>
        new(
            dataManager,
            new EntityTrackerFactory(NullLoggerFactory.Instance),
            [new EnemyAlertRulesProvider(settingsService, dataManager)],
            [new DoorAlertRulesProvider(settingsService, dataManager)],
            [new PlayerAlertRulesProvider(settingsService, dataManager)],
            [new LobbySlotAlertRulesProvider(settingsService)]
        );

    private static DecodedInGamePlayer CreatePlayer(Ulid playerId, double virusPercentage)
    {
        return new DecodedInGamePlayer
        {
            Id = playerId,
            IsEnabled = true,
            IsInGame = true,
            Name = "Kevin",
            CurHealth = 100,
            MaxHealth = 100,
            VirusPercentage = virusPercentage,
            MaxVirus = 100,
            CurVirus = (int)virusPercentage,
            Condition = "normal",
            Status = "Alive",
            RoomId = 1,
            RoomName = "Test Room",
        };
    }

    private sealed class FakeDataManager : IDataManager, ICurrentScenarioState, IDisposable
    {
        private readonly ReactiveProperty<DecodedDoor[]> _doors = new([]);
        private readonly ReactiveProperty<DecodedEnemy[]> _enemies = new([]);
        private readonly ReactiveProperty<DecodedInGamePlayer[]> _inGamePlayers = new([]);
        private readonly ReactiveProperty<InGameOverviewSnapshot> _inGameOverview = new(new InGameOverviewSnapshot());
        private readonly ReactiveProperty<DecodedInGameScenario> _inGameScenario = new(new DecodedInGameScenario());
        private readonly ReactiveProperty<DecodedLobbyRoom> _lobbyRoom = new(new DecodedLobbyRoom());
        private readonly ReactiveProperty<DecodedLobbyRoomPlayer[]> _lobbyPlayers = new([]);
        private readonly ReactiveProperty<DecodedLobbySlot[]> _lobbySlots = new([]);
        private readonly ReactiveProperty<bool> _isAtLobby = new(false);

        public DecodedDoor[] Doors => _doors.Value;
        public DecodedEnemy[] Enemies => _enemies.Value;
        public DecodedInGamePlayer[] InGamePlayers => _inGamePlayers.Value;
        public DecodedInGameScenario InGameScenario => _inGameScenario.Value;
        public DecodedLobbyRoom LobbyRoom => _lobbyRoom.Value;
        public DecodedLobbyRoomPlayer[] LobbyRoomPlayers => _lobbyPlayers.Value;
        public DecodedLobbySlot[] LobbySlots => _lobbySlots.Value;
        public bool IsAtLobby => _isAtLobby.Value;

        // ICurrentScenarioState
        string ICurrentScenarioState.ScenarioName => _inGameScenario.Value.ScenarioName;
        ScenarioStatus ICurrentScenarioState.Status => _inGameScenario.Value.Status;

        public Observable<DecodedDoor[]> DoorsObservable => _doors;
        public Observable<DecodedEnemy[]> EnemiesObservable => _enemies;
        public Observable<DecodedInGamePlayer[]> InGamePlayersObservable => _inGamePlayers;
        public Observable<InGameOverviewSnapshot> InGameOverviewObservable => _inGameOverview;
        public Observable<DecodedInGameScenario> InGameScenarioObservable => _inGameScenario;
        public Observable<DecodedLobbyRoom> LobbyRoomObservable => _lobbyRoom;
        public Observable<DecodedLobbyRoomPlayer[]> LobbyRoomPlayersObservable => _lobbyPlayers;
        public Observable<DecodedLobbySlot[]> LobbySlotsObservable => _lobbySlots;
        public Observable<bool> IsAtLobbyObservable => _isAtLobby;

        public ValueTask InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public void SetInGamePlayers(DecodedInGamePlayer[] players) => _inGamePlayers.Value = players;

        public void SetLobbyPresence(bool isAtLobby) => _isAtLobby.Value = isAtLobby;

        public void SetLobbySlots(DecodedLobbySlot[] slots) => _lobbySlots.Value = slots;

        public void Dispose()
        {
            _doors.Dispose();
            _enemies.Dispose();
            _inGamePlayers.Dispose();
            _inGameOverview.Dispose();
            _inGameScenario.Dispose();
            _lobbyRoom.Dispose();
            _lobbyPlayers.Dispose();
            _lobbySlots.Dispose();
            _isAtLobby.Dispose();
        }
    }

    private sealed class FakeAppSettingsService : IAppSettingsService
    {
        private readonly ReactiveProperty<OutbreakTrackerSettings> _settings;

        public string UserSettingsPath { get; } =
            Path.Combine(Path.GetTempPath(), "outbreaktracker2-fake-settings.json");

        public FakeAppSettingsService(OutbreakTrackerSettings? settings = null)
        {
            _settings = new ReactiveProperty<OutbreakTrackerSettings>(settings ?? new OutbreakTrackerSettings());
        }

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

        public ValueTask<OutbreakTrackerSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default)
        {
            _settings.Value = new OutbreakTrackerSettings();
            return ValueTask.FromResult(Current);
        }

        public void Dispose() => _settings.Dispose();
    }

    private sealed class FakeEntityTrackerFactory : IEntityTrackerFactory, IDisposable
    {
        public HashSet<Type> CreatedTypes { get; } = [];

        public FakeEntityTracker<DecodedEnemy> EnemyTracker { get; } = new();

        public FakeEntityTracker<DecodedDoor> DoorTracker { get; } = new();

        public FakeEntityTracker<DecodedInGamePlayer> PlayerTracker { get; } = new();

        public FakeEntityTracker<DecodedLobbySlot> LobbyTracker { get; } = new();

        public IEntityTracker<T> Create<T>(Observable<T[]> snapshots)
            where T : IHasId
        {
            CreatedTypes.Add(typeof(T));

            object tracker = typeof(T) switch
            {
                { } type when type == typeof(DecodedEnemy) => EnemyTracker,
                { } type when type == typeof(DecodedDoor) => DoorTracker,
                { } type when type == typeof(DecodedInGamePlayer) => PlayerTracker,
                { } type when type == typeof(DecodedLobbySlot) => LobbyTracker,
                _ => throw new NotSupportedException($"Unexpected tracker type {typeof(T).Name}"),
            };

            return (IEntityTracker<T>)tracker;
        }

        public void Dispose()
        {
            EnemyTracker.Dispose();
            DoorTracker.Dispose();
            PlayerTracker.Dispose();
            LobbyTracker.Dispose();
        }
    }

    private sealed class FakeEntityTracker<T> : IEntityTracker<T>
        where T : IHasId
    {
        private readonly Subject<AlertNotification> _alerts = new();

        public IEntityChangeSource<T> Changes { get; } = new FakeEntityChangeSource<T>();

        public Observable<AlertNotification> Alerts => _alerts;

        public void AddRule(IAlertRule<T> rule) { }

        public void AddAddedRule(IAlertRule<T> rule) { }

        public void AddRemovedRule(IAlertRule<T> rule) { }

        public void Dispose()
        {
            _alerts.Dispose();
            Changes.Dispose();
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

        public void Dispose()
        {
            _added.Dispose();
            _removed.Dispose();
            _updated.Dispose();
            _diffs.Dispose();
        }
    }
}

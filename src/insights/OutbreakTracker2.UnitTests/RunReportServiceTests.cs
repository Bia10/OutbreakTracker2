using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Reports;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.PCSX2.Client;
using R3;
using SukiUI.Toasts;

namespace OutbreakTracker2.UnitTests;

public sealed class RunReportServiceTests
{
    // ── helpers ─────────────────────────────────────────────────────────────

    private static RunReportService CreateService(
        FakeTrackerRegistry registry,
        FakeDataSource dataSource,
        FakeRunReportWriter? writer = null,
        TimeProvider? time = null,
        FakeToastService? toastService = null,
        ILogger<RunReportService>? logger = null,
        IRunReportCollectionDiffProcessor<DecodedLobbySlot>? lobbySlotProcessor = null,
        IRunReportCollectionDiffProcessor<DecodedInGamePlayer>? playerProcessor = null,
        IRunReportCollectionDiffProcessor<DecodedEnemy>? enemyProcessor = null,
        IRunReportCollectionDiffProcessor<DecodedDoor>? doorProcessor = null,
        IRunReportScenarioProcessor? scenarioProcessor = null
    ) =>
        new(
            registry,
            dataSource,
            writer ?? new FakeRunReportWriter(),
            toastService ?? new FakeToastService(),
            time ?? TimeProvider.System,
            logger ?? NullLogger<RunReportService>.Instance,
            lobbySlotProcessor ?? new RunReportLobbySlotDiffProcessor(),
            playerProcessor ?? new RunReportPlayerDiffProcessor(),
            enemyProcessor ?? new RunReportEnemyDiffProcessor(logger ?? NullLogger<RunReportService>.Instance),
            doorProcessor ?? new RunReportDoorDiffProcessor(),
            scenarioProcessor ?? new RunReportScenarioProcessor()
        );

    private static DecodedInGamePlayer InGamePlayer(Ulid id, short hp = 100, short maxHp = 100) =>
        new()
        {
            Id = id,
            IsEnabled = true,
            IsInGame = true,
            Name = "Kevin",
            CurHealth = hp,
            MaxHealth = maxHp,
            VirusPercentage = 0.0,
            Status = "Alive",
            Condition = "normal",
            RoomId = 1,
        };

    private static DecodedEnemy AliveEnemy(Ulid id, ushort maxHp = 200) =>
        new()
        {
            Id = id,
            Enabled = 1,
            InGame = 1,
            SlotId = 0,
            NameId = 1, // non-zero: required by entity-identity kill guard; parses to unknown EnemyType (not invulnerable)
            RoomId = 1,
            Name = "Zombie",
            CurHp = maxHp,
            MaxHp = maxHp,
        };

    private static DecodedEnemy DeadEnemy(Ulid id, ushort maxHp = 200) => AliveEnemy(id, maxHp) with { CurHp = 0 };

    private static CollectionDiff<T> Added<T>(params T[] items)
        where T : IHasId => new(items, [], []);

    private static CollectionDiff<T> Removed<T>(params T[] items)
        where T : IHasId => new([], items, []);

    private static CollectionDiff<T> Changed<T>(T previous, T current)
        where T : IHasId => new([], [], [new EntityChange<T>(previous, current)]);

    private static CollectionDiff<T> Empty<T>()
        where T : IHasId => new([], [], []);

    // ── IsRunning ────────────────────────────────────────────────────────────

    [Test]
    public async Task IsRunning_IsFalse_Initially()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        using RunReportService svc = CreateService(registry, dataSource);

        await Assert.That(svc.IsRunning).IsFalse();
    }

    // ── Session auto-start via player diff ──────────────────────────────────

    [Test]
    public async Task Session_IsStarted_WhenPlayersJoin_AndScenarioIsInGame()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        using RunReportService svc = CreateService(registry, dataSource);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(Ulid.NewUlid())));

        await Assert.That(svc.IsRunning).IsTrue();
    }

    [Test]
    public async Task Session_IsNotStarted_WhenPlayersJoin_AndScenarioIsNotInGame()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        using RunReportService svc = CreateService(registry, dataSource);

        // Status stays ScenarioStatus.None (default)
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(Ulid.NewUlid())));

        await Assert.That(svc.IsRunning).IsFalse();
    }

    [Test]
    public async Task Session_IsNotStarted_WhenNonInGamePlayerJoins()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        using RunReportService svc = CreateService(registry, dataSource);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });

        DecodedInGamePlayer lobbyPlayer = new()
        {
            Id = Ulid.NewUlid(),
            IsEnabled = true,
            IsInGame = false,
            Name = "Idle",
        };
        registry.PlayerTracker.ChangesSource.Push(Added(lobbyPlayer));

        await Assert.That(svc.IsRunning).IsFalse();
    }

    // ── Session auto-start via scenario diff ────────────────────────────────

    [Test]
    public async Task Session_IsStarted_WhenScenarioBecomesInGame_AndPlayersAlreadyPresent()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        using RunReportService svc = CreateService(registry, dataSource);

        // Players join while status is None → no session start
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(Ulid.NewUlid())));
        await Assert.That(svc.IsRunning).IsFalse();

        // Status becomes InGame → session starts because players are present
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });

        await Assert.That(svc.IsRunning).IsTrue();
    }

    // ── Session auto-stop ────────────────────────────────────────────────────

    [Test]
    public async Task Session_IsStopped_WhenAllPlayersLeave_AndStatusIsNotTransitional()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        using RunReportService svc = CreateService(registry, dataSource);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        Ulid playerId = Ulid.NewUlid();
        DecodedInGamePlayer player = InGamePlayer(playerId);
        registry.PlayerTracker.ChangesSource.Push(Added(player));

        await Assert.That(svc.IsRunning).IsTrue();

        registry.PlayerTracker.ChangesSource.Push(Removed(player));

        await Assert.That(svc.IsRunning).IsFalse();
    }

    [Test]
    public async Task Session_IsNotStopped_WhenPlayersLeave_DuringTransitionalStatus()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        using RunReportService svc = CreateService(registry, dataSource);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        Ulid playerId = Ulid.NewUlid();
        DecodedInGamePlayer player = InGamePlayer(playerId);
        registry.PlayerTracker.ChangesSource.Push(Added(player));

        // Transition to loading state
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.TransitionLoading });

        // Players disappear during loading
        registry.PlayerTracker.ChangesSource.Push(Removed(player));

        await Assert.That(svc.IsRunning).IsTrue();
    }

    [Test]
    public async Task Session_IsStopped_WhenScenarioBecomesGameFinished()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        using RunReportService svc = CreateService(registry, dataSource);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(Ulid.NewUlid())));

        await Assert.That(svc.IsRunning).IsTrue();

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.GameFinished });

        await Assert.That(svc.IsRunning).IsFalse();
    }

    [Test]
    public async Task Session_IsStopped_WhenScenarioBecomesNone_WhileRunning()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        using RunReportService svc = CreateService(registry, dataSource);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(Ulid.NewUlid())));

        await Assert.That(svc.IsRunning).IsTrue();

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.None });

        await Assert.That(svc.IsRunning).IsFalse();
    }

    // ── Event emission ───────────────────────────────────────────────────────

    [Test]
    public async Task PlayerJoinedEvent_IsEmitted_WhenInGamePlayerJoins_DuringActiveSession()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(Ulid.NewUlid(), hp: 100, maxHp: 100)));

        await Assert.That(received.OfType<PlayerJoinedEvent>().Count()).IsEqualTo(1);

        PlayerJoinedEvent join = received.OfType<PlayerJoinedEvent>().Single();
        await Assert.That(join.PlayerName).IsEqualTo("Kevin");
        await Assert.That((int)join.InitialHealth).IsEqualTo(100);
    }

    [Test]
    public async Task PlayerLeftEvent_IsEmitted_WhenActivePlayerLeaves()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        Ulid id = Ulid.NewUlid();
        DecodedInGamePlayer player = InGamePlayer(id);
        registry.PlayerTracker.ChangesSource.Push(Added(player));
        registry.PlayerTracker.ChangesSource.Push(Removed(player));

        await Assert.That(received.OfType<PlayerLeftEvent>().Count()).IsEqualTo(1);
        await Assert.That(received.OfType<PlayerLeftEvent>().Single().PlayerName).IsEqualTo("Kevin");
    }

    [Test]
    public async Task PlayerHealthChangedEvent_IsEmitted_OnHealthDecrease()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid id = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(id, hp: 100, maxHp: 100)));
        registry.PlayerTracker.ChangesSource.Push(
            Changed(InGamePlayer(id, hp: 100, maxHp: 100), InGamePlayer(id, hp: 60, maxHp: 100))
        );

        PlayerHealthChangedEvent? dmg = received.OfType<PlayerHealthChangedEvent>().FirstOrDefault();
        await Assert.That(dmg).IsNotNull();
        await Assert.That(dmg!.IsDamage).IsTrue();
        await Assert.That((int)(dmg.OldHealth - dmg.NewHealth)).IsEqualTo(40);
    }

    [Test]
    public async Task PlayerInventoryChangedEvent_UsesEmptyAndUnknownSlotResolution()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(
            new DecodedInGameScenario
            {
                Status = ScenarioStatus.InGame,
                Items = new DecodedItem[GameConstants.MaxItems],
            }
        );

        DecodedInGamePlayer previous = InGamePlayer(playerId) with { Inventory = InventorySnapshot.Empty };
        DecodedInGamePlayer current = previous with { Inventory = new InventorySnapshot(0x21, 0, 0, 0) };

        registry.PlayerTracker.ChangesSource.Push(Added(previous));
        registry.PlayerTracker.ChangesSource.Push(Changed(previous, current));

        PlayerInventoryChangedEvent? evt = received.OfType<PlayerInventoryChangedEvent>().FirstOrDefault();
        await Assert.That(evt).IsNotNull();
        await Assert.That(evt!.OldItemName).IsEqualTo("Empty");
        await Assert.That((int)evt.OldItemId).IsEqualTo(0);
        await Assert.That(evt.NewItemName).IsEqualTo("Unknown");
        await Assert.That((int)evt.NewItemId).IsEqualTo(0x21);
    }

    [Test]
    public async Task EnemySpawnedEvent_IsEmitted_WhenEnemyAdded_DuringInGame()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        registry.EnemyTracker.ChangesSource.Push(Added(AliveEnemy(Ulid.NewUlid(), maxHp: 150)));

        await Assert.That(received.OfType<EnemySpawnedEvent>().Count()).IsEqualTo(1);
        await Assert.That((int)received.OfType<EnemySpawnedEvent>().Single().MaxHp).IsEqualTo(150);
    }

    [Test]
    public async Task EnemySpawnedEvent_IsNotEmitted_WhenNotInGame()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        // Status is None (default) — enemy diffs should be ignored
        registry.EnemyTracker.ChangesSource.Push(Added(AliveEnemy(Ulid.NewUlid())));

        await Assert.That(received.OfType<EnemySpawnedEvent>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task EnemyKilledEvent_IsEmitted_WhenEnemyRemovedWithZeroHp()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        Ulid enemyId = Ulid.NewUlid();
        registry.EnemyTracker.ChangesSource.Push(Removed(DeadEnemy(enemyId, maxHp: 200)));

        await Assert.That(received.OfType<EnemyKilledEvent>().Count()).IsEqualTo(1);
        await Assert.That(received.OfType<EnemyKilledEvent>().Single().EnemyName).IsEqualTo("Zombie");
    }

    [Test]
    public async Task EnemyDespawnedEvent_IsEmitted_WhenEnemyRemovedWithHighHp()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        Ulid enemyId = Ulid.NewUlid();
        DecodedEnemy highHpEnemy = AliveEnemy(enemyId, maxHp: 500) with { CurHp = 300 };
        registry.EnemyTracker.ChangesSource.Push(Removed(highHpEnemy));

        await Assert.That(received.OfType<EnemyDespawnedEvent>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task EnemyDamagedEvent_IsNotEmitted_WhenPreviousHpSnapshotIsImpossible()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        RecordingLogger<RunReportService> logger = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource, logger: logger);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        Ulid enemyId = Ulid.NewUlid();
        DecodedEnemy previous = AliveEnemy(enemyId, maxHp: 1450) with { CurHp = 0xffff, Name = "Megabyte" };
        DecodedEnemy current = previous with { CurHp = 1450 };
        registry.EnemyTracker.ChangesSource.Push(Changed(previous, current));

        await Assert.That(received.OfType<EnemyDamagedEvent>().Count()).IsEqualTo(0);
        await Assert
            .That(
                logger.Messages.Any(message =>
                    message.Contains("Excluded faulty enemy run-report damage event", StringComparison.Ordinal)
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task EnemyDamagedEvent_IsNotEmitted_WhenDamageExceedsSupportedRange()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        RecordingLogger<RunReportService> logger = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource, logger: logger);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        Ulid enemyId = Ulid.NewUlid();
        DecodedEnemy previous = AliveEnemy(enemyId, maxHp: 20000) with { CurHp = 20000, Name = "Megabyte" };
        DecodedEnemy current = previous with { CurHp = 9000 };
        registry.EnemyTracker.ChangesSource.Push(Changed(previous, current));

        await Assert.That(received.OfType<EnemyDamagedEvent>().Count()).IsEqualTo(0);
        await Assert
            .That(
                logger.Messages.Any(message =>
                    message.Contains("enemy damage exceeds the supported report range", StringComparison.Ordinal)
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task DoorStateChangedEvent_IsEmitted_OnDoorStatusChange_DuringInGame()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        Ulid doorId = Ulid.NewUlid();
        DecodedDoor closedDoor = new()
        {
            Id = doorId,
            Status = "Locked",
            Hp = 100,
            Flag = 0,
        };
        DecodedDoor openDoor = closedDoor with { Status = "Open" };
        registry.DoorTracker.ChangesSource.Push(Changed(closedDoor, openDoor));

        await Assert.That(received.OfType<DoorStateChangedEvent>().Count()).IsEqualTo(1);

        DoorStateChangedEvent evt = received.OfType<DoorStateChangedEvent>().Single();
        await Assert.That(evt.OldStatus).IsEqualTo("Locked");
        await Assert.That(evt.NewStatus).IsEqualTo("Open");
    }

    // ── CompletedReports ─────────────────────────────────────────────────────

    [Test]
    public async Task CompletedReport_IsEmitted_WhenSessionStops()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        FakeRunReportWriter writer = new();
        List<RunReport> reports = [];
        using RunReportService svc = CreateService(registry, dataSource, writer);
        using IDisposable sub = svc.CompletedReports.Subscribe(reports.Add);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        Ulid playerId = Ulid.NewUlid();
        DecodedInGamePlayer player = InGamePlayer(playerId);
        registry.PlayerTracker.ChangesSource.Push(Added(player));
        registry.PlayerTracker.ChangesSource.Push(Removed(player));

        await Assert.That(reports.Count).IsEqualTo(1);
        await Assert.That(reports[0].Events.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task CompletedReport_ContainsPlayerJoinedAndLeftEvents()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunReport> reports = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.CompletedReports.Subscribe(reports.Add);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        Ulid playerId = Ulid.NewUlid();
        DecodedInGamePlayer player = InGamePlayer(playerId);
        registry.PlayerTracker.ChangesSource.Push(Added(player));
        registry.PlayerTracker.ChangesSource.Push(Removed(player));

        await Assert.That(reports.Count).IsEqualTo(1);
        IReadOnlyList<RunEvent> events = reports[0].Events;
        await Assert.That(events.OfType<PlayerJoinedEvent>().Count()).IsEqualTo(1);
        await Assert.That(events.OfType<PlayerLeftEvent>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task CompletedReport_WriterIsCalled_AfterSessionStop()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        FakeRunReportWriter writer = new();
        using RunReportService svc = CreateService(registry, dataSource, writer);

        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        Ulid playerId = Ulid.NewUlid();
        DecodedInGamePlayer player = InGamePlayer(playerId);
        registry.PlayerTracker.ChangesSource.Push(Added(player));
        registry.PlayerTracker.ChangesSource.Push(Removed(player));

        // Give the ContinueWith fire-and-forget a moment to execute
        await Task.Delay(50);

        await Assert.That(writer.Written.Count).IsEqualTo(1);
    }

    [Test]
    public async Task EnemyKilledEvent_IsEmitted_WhenEnemyHpDropsToZero_ViaChanged()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        // Enemy is alive, then HP drops to 0x0 — arrives as Changed, not Removed.
        Ulid enemyId = Ulid.NewUlid();
        DecodedEnemy alive = AliveEnemy(enemyId, maxHp: 200);
        DecodedEnemy dead = alive with { CurHp = 0 };
        registry.EnemyTracker.ChangesSource.Push(Changed(alive, dead));

        await Assert.That(received.OfType<EnemyKilledEvent>().Count()).IsEqualTo(1);
        await Assert.That(received.OfType<EnemyKilledEvent>().Single().EnemyName).IsEqualTo("Zombie");
    }

    [Test]
    public async Task EnemyKilledEvent_IsEmitted_WhenCurHpBecomesDeadRange_0xffff_ViaChanged()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        // The game's most common death state is CurHp = 0xffff (>= 0x8000), not 0.
        Ulid enemyId = Ulid.NewUlid();
        DecodedEnemy alive = AliveEnemy(enemyId, maxHp: 1800);
        DecodedEnemy dead = alive with { CurHp = 0xffff };
        registry.EnemyTracker.ChangesSource.Push(Changed(alive, dead));

        await Assert.That(received.OfType<EnemyKilledEvent>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task EnemyKilledEvent_IsNotEmitted_WhenEntityChanges_ViaDifferentNameId()
    {
        // During in-game room loads, the 80-slot enemy array retains the same Ulids but
        // different enemies load into the slots. A slot going from alive-NameId-A to
        // dead-NameId-B must NOT emit a kill (the guard: prev.NameId == curr.NameId).
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        Ulid slotId = Ulid.NewUlid();
        DecodedEnemy entityA = AliveEnemy(slotId, maxHp: 500) with { NameId = 5 };
        DecodedEnemy entityB = entityA with { NameId = 7, CurHp = 0xffff }; // different entity in same slot
        registry.EnemyTracker.ChangesSource.Push(Changed(entityA, entityB));

        await Assert.That(received.OfType<EnemyKilledEvent>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task EnemyDespawnedEvent_IsEmitted_WhenEnemyDisabled_WithPositiveHp_ViaChanged()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        Ulid enemyId = Ulid.NewUlid();
        DecodedEnemy alive = AliveEnemy(enemyId, maxHp: 500) with { CurHp = 300 };
        DecodedEnemy despawned = alive with { Enabled = 0 };
        registry.EnemyTracker.ChangesSource.Push(Changed(alive, despawned));

        await Assert.That(received.OfType<EnemyDespawnedEvent>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Session_IsNotStopped_WhenPlayersLeave_DuringUnknown8To11Status()
    {
        foreach (
            ScenarioStatus unknownStatus in new[]
            {
                ScenarioStatus.Unknown8,
                ScenarioStatus.Unknown9,
                ScenarioStatus.Unknown10,
                ScenarioStatus.Unknown11,
            }
        )
        {
            using FakeTrackerRegistry registry = new();
            using FakeDataSource dataSource = new();
            using RunReportService svc = CreateService(registry, dataSource);

            dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
            Ulid playerId = Ulid.NewUlid();
            DecodedInGamePlayer player = InGamePlayer(playerId);
            registry.PlayerTracker.ChangesSource.Push(Added(player));

            // Transition to online-specific unknown status
            dataSource.SetScenario(new DecodedInGameScenario { Status = unknownStatus });

            // Players temporarily disappear during this status
            registry.PlayerTracker.ChangesSource.Push(Removed(player));

            await Assert.That(svc.IsRunning).IsTrue().Because($"{unknownStatus} should be treated as transitional");
        }
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    [Test]
    public void Dispose_DoesNotThrow()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        RunReportService svc = CreateService(registry, dataSource);
        svc.Dispose();
    }

    [Test]
    public async Task WriteFailure_ShowsErrorToast()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        FakeRunReportWriter writer = new()
        {
            WriteException = new InvalidOperationException("Simulated report write failure."),
        };
        FakeToastService toastService = new();
        using RunReportService svc = CreateService(registry, dataSource, writer, toastService: toastService);

        dataSource.SetScenario(
            new DecodedInGameScenario { Status = ScenarioStatus.InGame, ScenarioName = "Wild Things" }
        );
        DecodedInGamePlayer player = InGamePlayer(Ulid.NewUlid());

        registry.PlayerTracker.ChangesSource.Push(Added(player));
        registry.PlayerTracker.ChangesSource.Push(Removed(player));

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
        string title = await toastService.LastErrorTitle.Task.WaitAsync(timeout.Token);
        string content = await toastService.LastErrorContent.Task.WaitAsync(timeout.Token);

        await Assert.That(title).IsEqualTo("Run report export failed");
        await Assert.That(content).Contains("Wild Things");
    }

    [Test]
    public async Task Service_DelegatesIncomingStreams_ToInjectedProcessors()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        RecordingCollectionDiffProcessor<DecodedLobbySlot> lobbySlotProcessor = new();
        RecordingCollectionDiffProcessor<DecodedInGamePlayer> playerProcessor = new();
        RecordingCollectionDiffProcessor<DecodedEnemy> enemyProcessor = new();
        RecordingCollectionDiffProcessor<DecodedDoor> doorProcessor = new();
        RecordingScenarioProcessor scenarioProcessor = new();
        using RunReportService svc = CreateService(
            registry,
            dataSource,
            lobbySlotProcessor: lobbySlotProcessor,
            playerProcessor: playerProcessor,
            enemyProcessor: enemyProcessor,
            doorProcessor: doorProcessor,
            scenarioProcessor: scenarioProcessor
        );

        DecodedLobbySlot lobbySlot = new() { Id = Ulid.NewUlid(), ScenarioId = "wild-things" };
        DecodedDoor closedDoor = new()
        {
            Id = Ulid.NewUlid(),
            Status = "Locked",
            Hp = 100,
            Flag = 0,
        };
        DecodedDoor openDoor = closedDoor with { Status = "Open" };

        registry.LobbySlotTracker.ChangesSource.Push(Added(lobbySlot));
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(Ulid.NewUlid())));
        registry.EnemyTracker.ChangesSource.Push(Added(AliveEnemy(Ulid.NewUlid())));
        registry.DoorTracker.ChangesSource.Push(Changed(closedDoor, openDoor));
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });

        await Assert.That(lobbySlotProcessor.CallCount).IsEqualTo(1);
        await Assert.That(playerProcessor.CallCount).IsEqualTo(1);
        await Assert.That(enemyProcessor.CallCount).IsEqualTo(1);
        await Assert.That(doorProcessor.CallCount).IsEqualTo(1);
        await Assert.That(scenarioProcessor.CallCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(scenarioProcessor.LastScenario?.Status).IsEqualTo(ScenarioStatus.InGame);
    }

    // ── Item pickup/drop player name resolution ──────────────────────────────

    [Test]
    public async Task ItemPickedUpEvent_ResolvesHolderName_WhenPlayerIsEnabledButNotInGame()
    {
        // Reproduces the "ghost player" bug: a player slot with IsEnabled=true but IsInGame=false
        // (e.g. an NPC-controlled character) was never in ActivePlayersBySlot, so the old code
        // fell back to "Player N" and invented a phantom player in the report.
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        // Human player at slot 0 starts the session.
        Ulid humanId = Ulid.NewUlid();
        DecodedInGamePlayer humanPlayer = InGamePlayer(humanId) with { SlotIndex = 0, Name = "Karl" };
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(humanPlayer));

        // NPC player: enabled but NOT in-game (AI-controlled slot 2).
        Ulid npcId = Ulid.NewUlid();
        DecodedInGamePlayer npcPlayer = new()
        {
            Id = npcId,
            IsEnabled = true,
            IsInGame = false,
            Name = "Cindy",
            SlotIndex = 2,
            CurHealth = 100,
            MaxHealth = 100,
        };
        registry.PlayerTracker.ChangesSource.Push(Added(npcPlayer));

        // Simulate a ground item transitioning to PickedUp=3 (1-based → slot index 2 = Cindy).
        DecodedItem[] previousItems =
        [
            new DecodedItem
            {
                SlotIndex = 0,
                TypeName = "Green Herb",
                PickedUp = 0,
                RoomId = 1,
            },
        ];
        DecodedItem[] currentItems =
        [
            new DecodedItem
            {
                SlotIndex = 0,
                TypeName = "Green Herb",
                PickedUp = 3,
                RoomId = 1,
            },
        ];
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame, Items = previousItems });
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame, Items = currentItems });

        ItemPickedUpEvent? evt = received.OfType<ItemPickedUpEvent>().FirstOrDefault();
        await Assert.That(evt).IsNotNull();
        await Assert.That(evt!.PickedUpByName).IsEqualTo("Cindy");
    }

    [Test]
    public async Task ItemDroppedEvent_ResolvesHolderName_WhenPlayerIsEnabledButNotInGame()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid humanId = Ulid.NewUlid();
        DecodedInGamePlayer humanPlayer = InGamePlayer(humanId) with { SlotIndex = 0, Name = "Karl" };
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(humanPlayer));

        Ulid npcId = Ulid.NewUlid();
        DecodedInGamePlayer npcPlayer = new()
        {
            Id = npcId,
            IsEnabled = true,
            IsInGame = false,
            Name = "George",
            SlotIndex = 3,
            CurHealth = 100,
            MaxHealth = 100,
        };
        registry.PlayerTracker.ChangesSource.Push(Added(npcPlayer));

        // George (slot 3, PickedUp=4) drops the item.
        DecodedItem[] previousItems =
        [
            new DecodedItem
            {
                SlotIndex = 0,
                TypeName = "Knife",
                PickedUp = 4,
                RoomId = 2,
            },
        ];
        DecodedItem[] currentItems =
        [
            new DecodedItem
            {
                SlotIndex = 0,
                TypeName = "Knife",
                PickedUp = 0,
                RoomId = 2,
            },
        ];
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame, Items = previousItems });
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame, Items = currentItems });

        ItemDroppedEvent? evt = received.OfType<ItemDroppedEvent>().FirstOrDefault();
        await Assert.That(evt).IsNotNull();
        await Assert.That(evt!.PreviousHolder).IsEqualTo("George");
    }

    [Test]
    public async Task ItemPickedUpEvent_LogsWarning_WhenPickedUpSlotIsUnresolvable()
    {
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        RecordingLogger<RunReportService> logger = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource, logger: logger);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid humanId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(humanId) with { SlotIndex = 0 }));

        // PickedUp=3 but no player at slot index 2.
        DecodedItem[] previousItems =
        [
            new DecodedItem
            {
                SlotIndex = 0,
                TypeName = "Shotgun Shells",
                PickedUp = 0,
                RoomId = 1,
            },
        ];
        DecodedItem[] currentItems =
        [
            new DecodedItem
            {
                SlotIndex = 0,
                TypeName = "Shotgun Shells",
                PickedUp = 3,
                RoomId = 1,
            },
        ];
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame, Items = previousItems });
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame, Items = currentItems });

        ItemPickedUpEvent? evt = received.OfType<ItemPickedUpEvent>().FirstOrDefault();
        await Assert.That(evt).IsNotNull();
        // Name is empty — event uses anonymous description, no ghost player created.
        await Assert.That(evt!.PickedUpByName).IsEmpty();
        // A warning must be logged so the developer knows this is a bug.
        await Assert
            .That(logger.Messages.Any(m => m.Contains("Could not resolve holder name", StringComparison.Ordinal)))
            .IsTrue();
    }

    // ── Door HP reset ─────────────────────────────────────────────────────────

    [Test]
    public async Task DoorDamagedEvent_ReportsLethalDamage_WhenDoorHpResetsAfterReachingZero()
    {
        // RE: Outbreak doors don't actually die — when HP reaches 0 the game resets the door to
        // 500 HP and opens it. Because the poller samples at intervals, we may see the change as
        // OldHp=200, NewHp=500, which causes ushort subtraction to underflow (65236 instead of 200).
        // The processor should clamp NewHp to 0 whenever curr.Hp > prev.Hp so that the damage
        // event correctly reports 200 → 0, damage = 200.
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        Ulid doorId = Ulid.NewUlid();
        DecodedDoor damagedDoor = new()
        {
            Id = doorId,
            Status = "Locked",
            Hp = 200,
            Flag = 1,
            SlotId = 9,
        };
        // Simulate the poll catching the door after it reset: HP went 200 → 0 → 500
        DecodedDoor resetDoor = damagedDoor with
        {
            Hp = 500,
            Status = "Unlocked",
        };

        registry.DoorTracker.ChangesSource.Push(Changed(damagedDoor, resetDoor));

        DoorDamagedEvent? evt = received.OfType<DoorDamagedEvent>().FirstOrDefault();
        await Assert.That(evt).IsNotNull();
        await Assert.That(evt!.OldHp).IsEqualTo((ushort)200);
        // NewHp must be clamped to 0, not the raw reset value (500)
        await Assert.That(evt.NewHp).IsEqualTo((ushort)0);
        await Assert.That(evt.Damage).IsEqualTo((ushort)200);
    }

    [Test]
    public async Task DoorDamagedEvent_ReportsActualDamage_ForNormalDamageHit()
    {
        // Ensure that normal door damage (HP decreasing) is still reported accurately.
        using FakeTrackerRegistry registry = new();
        using FakeDataSource dataSource = new();
        List<RunEvent> received = [];
        using RunReportService svc = CreateService(registry, dataSource);
        using IDisposable sub = svc.Events.Subscribe(received.Add);

        Ulid playerId = Ulid.NewUlid();
        dataSource.SetScenario(new DecodedInGameScenario { Status = ScenarioStatus.InGame });
        registry.PlayerTracker.ChangesSource.Push(Added(InGamePlayer(playerId)));

        Ulid doorId = Ulid.NewUlid();
        DecodedDoor before = new()
        {
            Id = doorId,
            Status = "Locked",
            Hp = 800,
            Flag = 1,
            SlotId = 9,
        };
        DecodedDoor after = before with { Hp = 500 };

        registry.DoorTracker.ChangesSource.Push(Changed(before, after));

        DoorDamagedEvent? evt = received.OfType<DoorDamagedEvent>().FirstOrDefault();
        await Assert.That(evt).IsNotNull();
        await Assert.That(evt!.OldHp).IsEqualTo((ushort)800);
        await Assert.That(evt.NewHp).IsEqualTo((ushort)500);
        await Assert.That(evt.Damage).IsEqualTo((ushort)300);
    }

    private sealed class FakeTrackerRegistry : ITrackerRegistry, IDisposable
    {
        public FakeEntityTracker<DecodedEnemy> EnemyTracker { get; } = new();
        public FakeEntityTracker<DecodedDoor> DoorTracker { get; } = new();
        public FakeEntityTracker<DecodedInGamePlayer> PlayerTracker { get; } = new();
        public FakeEntityTracker<DecodedLobbySlot> LobbySlotTracker { get; } = new();

        public IReadOnlyEntityTracker<DecodedEnemy> Enemies => EnemyTracker;
        public IReadOnlyEntityTracker<DecodedDoor> Doors => DoorTracker;
        public IReadOnlyEntityTracker<DecodedInGamePlayer> Players => PlayerTracker;
        public IReadOnlyEntityTracker<DecodedLobbySlot> LobbySlots => LobbySlotTracker;

        public IEntityChangeSource<DecodedEnemy> EnemyChanges => EnemyTracker.ChangesSource;
        public IEntityChangeSource<DecodedDoor> DoorChanges => DoorTracker.ChangesSource;
        public IEntityChangeSource<DecodedInGamePlayer> PlayerChanges => PlayerTracker.ChangesSource;
        public IEntityChangeSource<DecodedLobbySlot> LobbySlotChanges => LobbySlotTracker.ChangesSource;

        private readonly Subject<AlertNotification> _allAlerts = new();
        public Observable<AlertNotification> AllAlerts => _allAlerts;

        public void Dispose()
        {
            EnemyTracker.Dispose();
            DoorTracker.Dispose();
            PlayerTracker.Dispose();
            LobbySlotTracker.Dispose();
            _allAlerts.Dispose();
        }
    }

    private sealed class FakeEntityTracker<T> : IEntityTracker<T>, IDisposable
        where T : IHasId
    {
        private readonly Subject<AlertNotification> _alerts = new();

        public FakeEntityChangeSource<T> ChangesSource { get; } = new();

        public IEntityChangeSource<T> Changes => ChangesSource;
        public Observable<AlertNotification> Alerts => _alerts;

        public void AddRule(IUpdatedAlertRule<T> rule) { }

        public void AddAddedRule(IAddedAlertRule<T> rule) { }

        public void AddRemovedRule(IRemovedAlertRule<T> rule) { }

        public void Dispose()
        {
            _alerts.Dispose();
            ChangesSource.Dispose();
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose() { }
        }
    }

    private sealed class FakeEntityChangeSource<T> : IEntityChangeSource<T>, IDisposable
        where T : IHasId
    {
        private readonly Subject<CollectionDiff<T>> _diffs = new();

        public Observable<T> Added => _diffs.SelectMany(d => d.Added.ToObservable());
        public Observable<T> Removed => _diffs.SelectMany(d => d.Removed.ToObservable());
        public Observable<EntityChange<T>> Updated => _diffs.SelectMany(d => d.Changed.ToObservable());
        public Observable<CollectionDiff<T>> Diffs => _diffs;

        public void Push(CollectionDiff<T> diff) => _diffs.OnNext(diff);

        public void Dispose() => _diffs.Dispose();
    }

    private sealed class FakeDataSource : IDataManager, IDisposable
    {
        private readonly ReactiveProperty<DecodedInGameScenario> _scenario = new(new DecodedInGameScenario());

        private readonly ReactiveProperty<DecodedDoor[]> _doors = new([]);
        private readonly ReactiveProperty<DecodedEnemy[]> _enemies = new([]);
        private readonly ReactiveProperty<DecodedInGamePlayer[]> _players = new([]);
        private readonly ReactiveProperty<InGameOverviewSnapshot> _overview = new(new InGameOverviewSnapshot());
        private readonly ReactiveProperty<DecodedLobbyRoom> _lobbyRoom = new(new DecodedLobbyRoom());
        private readonly ReactiveProperty<DecodedLobbyRoomPlayer[]> _lobbyPlayers = new([]);
        private readonly ReactiveProperty<DecodedLobbySlot[]> _lobbySlots = new([]);
        private readonly ReactiveProperty<bool> _isAtLobby = new(false);

        // IDataObservableSource
        Observable<DecodedDoor[]> IDataObservableSource.DoorsObservable => _doors;
        Observable<DecodedEnemy[]> IDataObservableSource.EnemiesObservable => _enemies;
        Observable<DecodedInGamePlayer[]> IDataObservableSource.InGamePlayersObservable => _players;
        Observable<InGameOverviewSnapshot> IDataObservableSource.InGameOverviewObservable => _overview;
        Observable<DecodedInGameScenario> IDataObservableSource.InGameScenarioObservable => _scenario;
        Observable<DecodedLobbyRoom> IDataObservableSource.LobbyRoomObservable => _lobbyRoom;
        Observable<DecodedLobbyRoomPlayer[]> IDataObservableSource.LobbyRoomPlayersObservable => _lobbyPlayers;
        Observable<DecodedLobbySlot[]> IDataObservableSource.LobbySlotsObservable => _lobbySlots;
        Observable<bool> IDataObservableSource.IsAtLobbyObservable => _isAtLobby;

        // IDataSnapshot
        DecodedDoor[] IDataSnapshot.Doors => _doors.Value;
        DecodedEnemy[] IDataSnapshot.Enemies => _enemies.Value;
        DecodedInGamePlayer[] IDataSnapshot.InGamePlayers => _players.Value;
        DecodedInGameScenario IDataSnapshot.InGameScenario => _scenario.Value;
        DecodedLobbyRoom IDataSnapshot.LobbyRoom => _lobbyRoom.Value;
        DecodedLobbyRoomPlayer[] IDataSnapshot.LobbyRoomPlayers => _lobbyPlayers.Value;
        DecodedLobbySlot[] IDataSnapshot.LobbySlots => _lobbySlots.Value;
        bool IDataSnapshot.IsAtLobby => _isAtLobby.Value;

        public ValueTask InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public void SetScenario(DecodedInGameScenario scenario) => _scenario.Value = scenario;

        public void Dispose()
        {
            _scenario.Dispose();
            _doors.Dispose();
            _enemies.Dispose();
            _players.Dispose();
            _overview.Dispose();
            _lobbyRoom.Dispose();
            _lobbyPlayers.Dispose();
            _lobbySlots.Dispose();
            _isAtLobby.Dispose();
        }
    }

    private sealed class FakeRunReportWriter : IRunReportWriter
    {
        public List<RunReport> Written { get; } = [];

        public Exception? WriteException { get; init; }

        public Task WriteAsync(RunReport report, CancellationToken cancellationToken = default)
        {
            Written.Add(report);

            if (WriteException is not null)
                return Task.FromException(WriteException);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeToastService : IToastService
    {
        public TaskCompletionSource<string> LastErrorTitle { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<string> LastErrorContent { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task InvokeInfoToastAsync(string content, string? title = "") => Task.CompletedTask;

        public Task InvokeSuccessToastAsync(string content, string? title = "") => Task.CompletedTask;

        public Task InvokeErrorToastAsync(string content, string? title = "")
        {
            LastErrorTitle.TrySetResult(title ?? string.Empty);
            LastErrorContent.TrySetResult(content);
            return Task.CompletedTask;
        }

        public Task InvokeWarningToastAsync(string content, string? title = "") => Task.CompletedTask;

        public ISukiToast CreateToast(string title, object content) => throw new NotSupportedException();

        public ISukiToast CreateInfoToastWithCancelButton(
            string content,
            object cancelButtonContent,
            Action<ISukiToast> onCanceledAction,
            string? title = ""
        ) => throw new NotSupportedException();

        public Task DismissToastAsync(ISukiToast toast) => Task.CompletedTask;
    }

    private sealed class RecordingCollectionDiffProcessor<T> : IRunReportCollectionDiffProcessor<T>
        where T : IHasId
    {
        public int CallCount { get; private set; }

        public void Process(CollectionDiff<T> diff, RunReportProcessingContext context) => CallCount++;
    }

    private sealed class RecordingScenarioProcessor : IRunReportScenarioProcessor
    {
        public int CallCount { get; private set; }

        public DecodedInGameScenario? LastScenario { get; private set; }

        public void Process(DecodedInGameScenario scenario, RunReportProcessingContext context)
        {
            CallCount++;
            LastScenario = scenario;
        }
    }
}

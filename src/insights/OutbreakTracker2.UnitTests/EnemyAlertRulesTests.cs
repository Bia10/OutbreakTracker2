using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class EnemyAlertRulesTests
{
    [Test]
    public async Task Register_AddsExpectedEnemyRuleGroups()
    {
        using CapturingEnemyTracker tracker = new();
        using FakeAppSettingsService settingsService = new();
        FakeCurrentScenarioState snapshot = new();

        EnemyAlertRules.Register(tracker, settingsService, snapshot);

        await Assert.That(tracker.AddedRules.Count).IsEqualTo(1);
        await Assert.That(tracker.Rules.Count).IsEqualTo(4);
        await Assert.That(tracker.RemovedRules.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AddedSpawnRule_Triggers_ForValidEnemy()
    {
        using CapturingEnemyTracker tracker = new();
        using FakeAppSettingsService settingsService = new();
        FakeCurrentScenarioState snapshot = new();

        EnemyAlertRules.Register(tracker, settingsService, snapshot);
        IAddedAlertRule<DecodedEnemy> spawnRule = tracker.AddedRules[0];

        bool triggered = spawnRule.ShouldTrigger(CreateEnemy());

        await Assert.That(triggered).IsTrue();
    }

    [Test]
    public async Task RemovedKilledRule_Triggers_ForDeadEnemyDuringScenario()
    {
        using CapturingEnemyTracker tracker = new();
        using FakeAppSettingsService settingsService = new();
        FakeCurrentScenarioState snapshot = new() { ScenarioName = "Wild Things", Status = ScenarioStatus.InGame };

        EnemyAlertRules.Register(tracker, settingsService, snapshot);
        IRemovedAlertRule<DecodedEnemy> killedRule = tracker.RemovedRules[0];

        bool triggered = killedRule.ShouldTrigger(CreateEnemy(curHp: 0));

        await Assert.That(triggered).IsTrue();
    }

    [Test]
    public async Task RoomChangeRule_Triggers_WhenEnemyMovesRooms()
    {
        using CapturingEnemyTracker tracker = new();
        using FakeAppSettingsService settingsService = new();
        FakeCurrentScenarioState snapshot = new();

        EnemyAlertRules.Register(tracker, settingsService, snapshot);
        IUpdatedAlertRule<DecodedEnemy> roomChangeRule = tracker.Rules[3];

        bool triggered = roomChangeRule.ShouldTrigger(CreateEnemy(roomId: 2), CreateEnemy(roomId: 1));

        await Assert.That(triggered).IsTrue();
    }

    private static DecodedEnemy CreateEnemy(byte roomId = 1, ushort curHp = 100) =>
        new()
        {
            Id = Ulid.NewUlid(),
            Enabled = 1,
            SlotId = 1,
            RoomId = roomId,
            NameId = 49,
            Name = "Hunter",
            CurHp = curHp,
            MaxHp = 100,
        };

    private sealed class CapturingEnemyTracker : IEntityTracker<DecodedEnemy>
    {
        private readonly Subject<AlertNotification> _alerts = new();

        public List<IAddedAlertRule<DecodedEnemy>> AddedRules { get; } = [];

        public List<IUpdatedAlertRule<DecodedEnemy>> Rules { get; } = [];

        public List<IRemovedAlertRule<DecodedEnemy>> RemovedRules { get; } = [];

        public IEntityChangeSource<DecodedEnemy> Changes { get; } = new FakeEntityChangeSource<DecodedEnemy>();

        public Observable<AlertNotification> Alerts => _alerts;

        public void AddRule(IUpdatedAlertRule<DecodedEnemy> rule) => Rules.Add(rule);

        public void AddAddedRule(IAddedAlertRule<DecodedEnemy> rule) => AddedRules.Add(rule);

        public void AddRemovedRule(IRemovedAlertRule<DecodedEnemy> rule) => RemovedRules.Add(rule);

        public void Dispose()
        {
            _alerts.Dispose();
            Changes.Dispose();
        }
    }

    private sealed class FakeAppSettingsService : IAppSettingsService
    {
        private readonly ReactiveProperty<OutbreakTrackerSettings> _settings = new(new OutbreakTrackerSettings());

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
            ValueTask.FromResult(new OutbreakTrackerSettings());

        public void Dispose() => _settings.Dispose();
    }

    private sealed class FakeCurrentScenarioState : ICurrentScenarioState
    {
        public string ScenarioName { get; init; } = string.Empty;

        public ScenarioStatus Status { get; init; } = ScenarioStatus.None;
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

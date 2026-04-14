using OutbreakTracker2.Application.Services.Reports;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.UnitTests;

public sealed class RunReportStatsTests
{
    [Test]
    public async Task ComputeStats_AccumulatesRepresentativeEvents_ThroughEventHierarchy()
    {
        DateTimeOffset startedAt = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
        Ulid playerOneId = Ulid.NewUlid();
        Ulid playerTwoId = Ulid.NewUlid();
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> contributors =
        [
            (playerOneId, "Kevin", 1.0f),
            (playerTwoId, "Jim", 3.0f),
        ];

        RunReport report = new(
            Ulid.NewUlid(),
            "wild-things",
            "Wild Things",
            Scenario.WildThings,
            startedAt,
            startedAt.AddMinutes(5),
            [
                new EnemySpawnedEvent(startedAt.AddSeconds(1), Ulid.NewUlid(), "Zombie", 2, 1, 200),
                new EnemyDamagedEvent(startedAt.AddSeconds(2), Ulid.NewUlid(), "Zombie", 2, 1, 20, 8, 20, contributors),
                new EnemyStatusChangedEvent(
                    startedAt.AddSeconds(3),
                    Ulid.NewUlid(),
                    "Zombie",
                    2,
                    1,
                    0x00,
                    0x01,
                    contributors
                ),
                new EnemyKilledEvent(startedAt.AddSeconds(4), Ulid.NewUlid(), "Zombie", 2, 1, contributors),
                new EnemyDespawnedEvent(startedAt.AddSeconds(5), Ulid.NewUlid(), "Crow", 4, 2, 12, 12),
                new EnemyRoomChangedEvent(startedAt.AddSeconds(6), Ulid.NewUlid(), "Hunter", 5, 1, 2),
                new PlayerHealthChangedEvent(startedAt.AddSeconds(7), playerOneId, "Kevin", 100, 80, 100),
                new PlayerHealthChangedEvent(startedAt.AddSeconds(8), playerOneId, "Kevin", 80, 90, 100),
                new PlayerVirusChangedEvent(startedAt.AddSeconds(9), playerOneId, "Kevin", 10.0, 35.0),
                new PlayerJoinedEvent(startedAt.AddSeconds(10), playerTwoId, "Jim", 100, 100, 0.0),
                new PlayerLeftEvent(startedAt.AddSeconds(11), playerTwoId, "Jim", 25, 40.0),
                new PlayerConditionChangedEvent(startedAt.AddSeconds(12), playerOneId, "Kevin", "Normal", "Danger"),
                new PlayerStatusChangedEvent(startedAt.AddSeconds(13), playerOneId, "Kevin", "Alive", "Bleed"),
                new PlayerEffectChangedEvent(startedAt.AddSeconds(14), playerOneId, "Kevin", "Bleed", true),
                new PlayerInventoryChangedEvent(
                    startedAt.AddSeconds(15),
                    playerOneId,
                    "Kevin",
                    InventoryKind.Main,
                    0,
                    0x00,
                    "Empty",
                    0x10,
                    "Handgun"
                ),
                new PlayerRoomChangedEvent(startedAt.AddSeconds(16), playerOneId, "Kevin", 1, 2),
                new DoorStateChangedEvent(startedAt.AddSeconds(17), Ulid.NewUlid(), 3, "open", "locked"),
                new DoorFlagChangedEvent(startedAt.AddSeconds(18), Ulid.NewUlid(), 3, 0x0001, 0x0002),
                new DoorDamagedEvent(startedAt.AddSeconds(19), Ulid.NewUlid(), 3, 100, 80),
                new ItemPickedUpEvent(startedAt.AddSeconds(20), "Blue Herb", 1, 2, "Kevin"),
                new ItemDroppedEvent(startedAt.AddSeconds(21), "Blue Herb", 1, 2, "Kevin"),
                new ItemQuantityChangedEvent(startedAt.AddSeconds(22), "Ammo", 5, 2, 15, 30),
                new ScenarioStatusChangedEvent(
                    startedAt.AddSeconds(23),
                    ScenarioStatus.Unknown8,
                    ScenarioStatus.Unknown9
                ),
            ]
        );

        RunReportStats stats = report.ComputeStats();

        await Assert.That(stats.TotalEnemySpawns).IsEqualTo(1);
        await Assert.That(stats.TotalEnemyKills).IsEqualTo(1);
        await Assert.That(stats.TotalDespawns).IsEqualTo(1);
        await Assert.That(stats.TotalEnemyDamageEvents).IsEqualTo(1);
        await Assert.That(stats.TotalEnemyStatusChanges).IsEqualTo(1);
        await Assert.That(stats.TotalEnemyActivations).IsEqualTo(1);
        await Assert.That(stats.TotalEnemyRoomTransitions).IsEqualTo(1);
        await Assert.That(stats.TotalDamageTaken).IsEqualTo(20);
        await Assert.That(stats.TotalPlayerHealthChanges).IsEqualTo(2);
        await Assert.That(stats.TotalPlayerHealingEvents).IsEqualTo(1);
        await Assert.That(stats.TotalPlayerVirusChanges).IsEqualTo(1);
        await Assert.That(stats.PeakVirusPercentage).IsEqualTo(35.0);
        await Assert.That(stats.TotalPlayerJoins).IsEqualTo(1);
        await Assert.That(stats.TotalPlayerLeaves).IsEqualTo(1);
        await Assert.That(stats.TotalPlayerConditionChanges).IsEqualTo(1);
        await Assert.That(stats.TotalPlayerStatusChanges).IsEqualTo(1);
        await Assert.That(stats.TotalPlayerEffectChanges).IsEqualTo(1);
        await Assert.That(stats.TotalPlayerInventoryChanges).IsEqualTo(1);
        await Assert.That(stats.TotalPlayerRoomTransitions).IsEqualTo(1);
        await Assert.That(stats.TotalDoorStateChanges).IsEqualTo(1);
        await Assert.That(stats.TotalDoorFlagChanges).IsEqualTo(1);
        await Assert.That(stats.TotalDoorDamageEvents).IsEqualTo(1);
        await Assert.That(stats.TotalItemPickups).IsEqualTo(1);
        await Assert.That(stats.TotalItemDrops).IsEqualTo(1);
        await Assert.That(stats.TotalItemQuantityChanges).IsEqualTo(1);
        await Assert.That(stats.TotalScenarioStatusChanges).IsEqualTo(1);
        await Assert.That(stats.EnemyDamageContributedByPlayer["Kevin"]).IsEqualTo(3);
        await Assert.That(stats.EnemyDamageContributedByPlayer["Jim"]).IsEqualTo(9);
        await Assert.That(stats.KillsContributedByPlayer["Kevin"]).IsEqualTo(1);
        await Assert.That(stats.KillsContributedByPlayer["Jim"]).IsEqualTo(1);
        await Assert.That(stats.Duration).IsEqualTo(TimeSpan.FromMinutes(5));
    }

    [Test]
    public async Task EnemyDamage_IsNotAttributed_WhenAllContributingPlayersHaveZeroPower()
    {
        // Ensures that Power=0 players (e.g. downed/loading) do not receive damage credit.
        // Previously the fallback `1f / players.Count` would distribute damage equally even
        // when no player had positive Power, inflating group-performance stats with unearned damage.
        DateTimeOffset start = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
        Ulid playerOneId = Ulid.NewUlid();
        Ulid playerTwoId = Ulid.NewUlid();

        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> zeroPowerContributors =
        [
            (playerOneId, "Kevin", 0f),
            (playerTwoId, "Jim", 0f),
        ];

        RunReport report = new(
            Ulid.NewUlid(),
            "wild-things",
            "Wild Things",
            Scenario.WildThings,
            start,
            start.AddMinutes(1),
            [
                new EnemyDamagedEvent(
                    start.AddSeconds(1),
                    Ulid.NewUlid(),
                    "Zombie",
                    2,
                    1,
                    200,
                    150,
                    200,
                    zeroPowerContributors
                ),
            ]
        );

        RunReportStats stats = report.ComputeStats();

        await Assert.That(stats.TotalEnemyDamageEvents).IsEqualTo(1);
        await Assert.That(stats.EnemyDamageContributedByPlayer.ContainsKey("Kevin")).IsFalse();
        await Assert.That(stats.EnemyDamageContributedByPlayer.ContainsKey("Jim")).IsFalse();
    }

    [Test]
    public async Task EnemyDamage_IsAttributedProportionally_WhenMixedPowerContributors()
    {
        // Sanity-check: a player with Power=0 mixed with a positive-Power player must not
        // receive a damage share; the positive-Power player gets 100% of the attribution.
        DateTimeOffset start = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
        Ulid aliveId = Ulid.NewUlid();
        Ulid downedId = Ulid.NewUlid();

        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> mixedContributors =
        [
            (aliveId, "Karl", 2.0f),
            (downedId, "Mark", 0f),
        ];

        RunReport report = new(
            Ulid.NewUlid(),
            "wild-things",
            "Wild Things",
            Scenario.WildThings,
            start,
            start.AddMinutes(1),
            [
                new EnemyDamagedEvent(
                    start.AddSeconds(1),
                    Ulid.NewUlid(),
                    "Zombie",
                    2,
                    1,
                    100,
                    80,
                    100,
                    mixedContributors
                ),
            ]
        );

        RunReportStats stats = report.ComputeStats();

        await Assert.That(stats.EnemyDamageContributedByPlayer["Karl"]).IsEqualTo(20);
        await Assert.That(stats.EnemyDamageContributedByPlayer.ContainsKey("Mark")).IsFalse();
    }
}

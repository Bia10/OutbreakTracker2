using OutbreakTracker2.Application.Services.Reports.Events;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed record RunReportStats
{
    public int TotalEnemySpawns { get; init; }

    public int TotalEnemyKills { get; init; }

    public int TotalDespawns { get; init; }

    public int TotalEnemyDamageEvents { get; init; }

    public int TotalEnemyStatusChanges { get; init; }

    public int TotalEnemyActivations { get; init; }

    public int TotalEnemyRoomTransitions { get; init; }

    public int TotalDamageTaken { get; init; }

    public int TotalPlayerHealthChanges { get; init; }

    public int TotalPlayerHealingEvents { get; init; }

    public int TotalPlayerVirusChanges { get; init; }

    public double PeakVirusPercentage { get; init; }

    public int TotalPlayerJoins { get; init; }

    public int TotalPlayerLeaves { get; init; }

    public int TotalPlayerConditionChanges { get; init; }

    public int TotalPlayerStatusChanges { get; init; }

    public int TotalPlayerEffectChanges { get; init; }

    public int TotalPlayerInventoryChanges { get; init; }

    public int TotalPlayerRoomTransitions { get; init; }

    public int TotalDoorStateChanges { get; init; }

    public int TotalDoorFlagChanges { get; init; }

    public int TotalDoorDamageEvents { get; init; }

    public int TotalItemPickups { get; init; }

    public int TotalItemDrops { get; init; }

    public int TotalItemQuantityChanges { get; init; }

    public int TotalScenarioStatusChanges { get; init; }

    public TimeSpan Duration { get; init; }

    public IReadOnlyDictionary<string, int> EnemyDamageContributedByPlayer { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> KillsContributedByPlayer { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public static RunReportStats From(RunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        int enemySpawns = 0;
        int kills = 0;
        int despawns = 0;
        int enemyDamageEvents = 0;
        int enemyStatusChanges = 0;
        int enemyActivations = 0;
        int enemyRoomTransitions = 0;
        int damageTaken = 0;
        int playerHealthChanges = 0;
        int playerHealingEvents = 0;
        int playerVirusChanges = 0;
        double peakVirus = 0.0;
        int playerJoins = 0;
        int playerLeaves = 0;
        int playerConditionChanges = 0;
        int playerStatusChanges = 0;
        int playerEffectChanges = 0;
        int playerInventoryChanges = 0;
        int playerRoomTransitions = 0;
        int doorChanges = 0;
        int doorFlagChanges = 0;
        int doorDamageEvents = 0;
        int itemPickups = 0;
        int itemDrops = 0;
        int itemQuantityChanges = 0;
        int scenarioStatusChanges = 0;
        Dictionary<string, int> damageByPlayer = [];
        Dictionary<string, int> killsByPlayer = [];

        foreach (RunEvent evt in report.Events)
        {
            switch (evt)
            {
                case EnemySpawnedEvent:
                    enemySpawns++;
                    break;
                case EnemyKilledEvent kill:
                    kills++;
                    AccumulateWeightedKill(kill.ContributingPlayers, killsByPlayer);
                    break;
                case EnemyDamagedEvent dmg:
                    enemyDamageEvents++;
                    AccumulateWeightedDamage(dmg.ContributingPlayers, dmg.Damage, damageByPlayer);
                    break;
                case EnemyDespawnedEvent:
                    despawns++;
                    break;
                case EnemyStatusChangedEvent enemyStatus:
                    enemyStatusChanges++;
                    if (enemyStatus.IsActivation)
                        enemyActivations++;
                    break;
                case EnemyRoomChangedEvent:
                    enemyRoomTransitions++;
                    break;
                case PlayerHealthChangedEvent { IsDamage: true } hp:
                    playerHealthChanges++;
                    damageTaken += hp.OldHealth - hp.NewHealth;
                    break;
                case PlayerHealthChangedEvent { IsHeal: true }:
                    playerHealthChanges++;
                    playerHealingEvents++;
                    break;
                case PlayerHealthChangedEvent:
                    playerHealthChanges++;
                    break;
                case PlayerVirusChangedEvent virus when virus.NewVirusPercentage > peakVirus:
                    playerVirusChanges++;
                    peakVirus = virus.NewVirusPercentage;
                    break;
                case PlayerVirusChangedEvent:
                    playerVirusChanges++;
                    break;
                case PlayerJoinedEvent:
                    playerJoins++;
                    break;
                case PlayerLeftEvent:
                    playerLeaves++;
                    break;
                case PlayerConditionChangedEvent:
                    playerConditionChanges++;
                    break;
                case PlayerStatusChangedEvent:
                    playerStatusChanges++;
                    break;
                case PlayerEffectChangedEvent:
                    playerEffectChanges++;
                    break;
                case PlayerInventoryChangedEvent:
                    playerInventoryChanges++;
                    break;
                case PlayerRoomChangedEvent:
                    playerRoomTransitions++;
                    break;
                case DoorStateChangedEvent:
                    doorChanges++;
                    break;
                case DoorFlagChangedEvent:
                    doorFlagChanges++;
                    break;
                case DoorDamagedEvent:
                    doorDamageEvents++;
                    break;
                case ItemPickedUpEvent:
                    itemPickups++;
                    break;
                case ItemDroppedEvent:
                    itemDrops++;
                    break;
                case ItemQuantityChangedEvent:
                    itemQuantityChanges++;
                    break;
                case ScenarioStatusChangedEvent:
                    scenarioStatusChanges++;
                    break;
            }
        }

        return new RunReportStats
        {
            TotalEnemySpawns = enemySpawns,
            TotalEnemyKills = kills,
            TotalDespawns = despawns,
            TotalEnemyDamageEvents = enemyDamageEvents,
            TotalEnemyStatusChanges = enemyStatusChanges,
            TotalEnemyActivations = enemyActivations,
            TotalEnemyRoomTransitions = enemyRoomTransitions,
            TotalDamageTaken = damageTaken,
            TotalPlayerHealthChanges = playerHealthChanges,
            TotalPlayerHealingEvents = playerHealingEvents,
            TotalPlayerVirusChanges = playerVirusChanges,
            PeakVirusPercentage = peakVirus,
            TotalPlayerJoins = playerJoins,
            TotalPlayerLeaves = playerLeaves,
            TotalPlayerConditionChanges = playerConditionChanges,
            TotalPlayerStatusChanges = playerStatusChanges,
            TotalPlayerEffectChanges = playerEffectChanges,
            TotalPlayerInventoryChanges = playerInventoryChanges,
            TotalPlayerRoomTransitions = playerRoomTransitions,
            TotalDoorStateChanges = doorChanges,
            TotalDoorFlagChanges = doorFlagChanges,
            TotalDoorDamageEvents = doorDamageEvents,
            TotalItemPickups = itemPickups,
            TotalItemDrops = itemDrops,
            TotalItemQuantityChanges = itemQuantityChanges,
            TotalScenarioStatusChanges = scenarioStatusChanges,
            Duration = report.Duration,
            EnemyDamageContributedByPlayer = damageByPlayer,
            KillsContributedByPlayer = killsByPlayer,
        };
    }

    // Distributes damage among contributors weighted by their Power stat.
    // If all Power values are zero (e.g. unknown) falls back to equal split.
    private static void AccumulateWeightedDamage(
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players,
        ushort totalDamage,
        Dictionary<string, int> accumulator
    )
    {
        if (players.Count == 0)
            return;

        float totalPower = 0f;
        foreach ((_, _, float p) in players)
            totalPower += p;

        foreach ((_, string name, float power) in players)
        {
            float share = totalPower > 0f ? power / totalPower : 1f / players.Count;
            int credited = (int)MathF.Round(totalDamage * share);
            accumulator[name] = accumulator.GetValueOrDefault(name) + credited;
        }
    }

    // A kill is credited to all contributors (each player in the room gets +1 kill share).
    // This mirrors most games' "assist = kill" convention since we cannot determine the finisher.
    private static void AccumulateWeightedKill(
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players,
        Dictionary<string, int> accumulator
    )
    {
        foreach ((_, string name, _) in players)
            accumulator[name] = accumulator.GetValueOrDefault(name) + 1;
    }
}

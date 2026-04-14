using OutbreakTracker2.Application.Services.Reports.Events;

namespace OutbreakTracker2.Application.Services.Reports;

internal sealed class RunReportStatsAccumulator : IRunEventStatsAccumulator
{
    private int _enemySpawns;
    private int _kills;
    private int _despawns;
    private int _enemyDamageEvents;
    private int _enemyStatusChanges;
    private int _enemyActivations;
    private int _enemyRoomTransitions;
    private int _damageTaken;
    private int _playerHealthChanges;
    private int _playerHealingEvents;
    private int _playerVirusChanges;
    private double _peakVirus;
    private int _playerJoins;
    private int _playerLeaves;
    private int _playerConditionChanges;
    private int _playerStatusChanges;
    private int _playerEffectChanges;
    private int _playerInventoryChanges;
    private int _playerRoomTransitions;
    private int _doorChanges;
    private int _doorFlagChanges;
    private int _doorDamageEvents;
    private int _itemPickups;
    private int _itemDrops;
    private int _itemQuantityChanges;
    private int _scenarioStatusChanges;
    private readonly Dictionary<string, int> _damageByPlayer = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _killsByPlayer = new(StringComparer.Ordinal);

    public void Accumulate(EnemySpawnedEvent evt) => _enemySpawns++;

    public void Accumulate(EnemyKilledEvent evt)
    {
        _kills++;
        AccumulateWeightedKill(evt.ContributingPlayers, _killsByPlayer);
    }

    public void Accumulate(EnemyDamagedEvent evt)
    {
        _enemyDamageEvents++;
        AccumulateWeightedDamage(evt.ContributingPlayers, evt.Damage, _damageByPlayer);
    }

    public void Accumulate(EnemyDespawnedEvent evt) => _despawns++;

    public void Accumulate(EnemyStatusChangedEvent evt)
    {
        _enemyStatusChanges++;
        if (evt.IsActivation)
            _enemyActivations++;
    }

    public void Accumulate(EnemyRoomChangedEvent evt) => _enemyRoomTransitions++;

    public void Accumulate(PlayerHealthChangedEvent evt)
    {
        _playerHealthChanges++;

        if (evt.IsDamage)
            _damageTaken += evt.OldHealth - evt.NewHealth;
        else if (evt.IsHeal)
            _playerHealingEvents++;
    }

    public void Accumulate(PlayerVirusChangedEvent evt)
    {
        _playerVirusChanges++;
        if (evt.NewVirusPercentage > _peakVirus)
            _peakVirus = evt.NewVirusPercentage;
    }

    public void Accumulate(PlayerJoinedEvent evt) => _playerJoins++;

    public void Accumulate(PlayerLeftEvent evt) => _playerLeaves++;

    public void Accumulate(PlayerConditionChangedEvent evt) => _playerConditionChanges++;

    public void Accumulate(PlayerStatusChangedEvent evt) => _playerStatusChanges++;

    public void Accumulate(PlayerEffectChangedEvent evt) => _playerEffectChanges++;

    public void Accumulate(PlayerInventoryChangedEvent evt) => _playerInventoryChanges++;

    public void Accumulate(PlayerRoomChangedEvent evt) => _playerRoomTransitions++;

    public void Accumulate(DoorStateChangedEvent evt) => _doorChanges++;

    public void Accumulate(DoorFlagChangedEvent evt) => _doorFlagChanges++;

    public void Accumulate(DoorDamagedEvent evt) => _doorDamageEvents++;

    public void Accumulate(ItemPickedUpEvent evt) => _itemPickups++;

    public void Accumulate(ItemDroppedEvent evt) => _itemDrops++;

    public void Accumulate(ItemQuantityChangedEvent evt) => _itemQuantityChanges++;

    public void Accumulate(ScenarioStatusChangedEvent evt) => _scenarioStatusChanges++;

    public RunReportStats Build(TimeSpan duration) =>
        new()
        {
            TotalEnemySpawns = _enemySpawns,
            TotalEnemyKills = _kills,
            TotalDespawns = _despawns,
            TotalEnemyDamageEvents = _enemyDamageEvents,
            TotalEnemyStatusChanges = _enemyStatusChanges,
            TotalEnemyActivations = _enemyActivations,
            TotalEnemyRoomTransitions = _enemyRoomTransitions,
            TotalDamageTaken = _damageTaken,
            TotalPlayerHealthChanges = _playerHealthChanges,
            TotalPlayerHealingEvents = _playerHealingEvents,
            TotalPlayerVirusChanges = _playerVirusChanges,
            PeakVirusPercentage = _peakVirus,
            TotalPlayerJoins = _playerJoins,
            TotalPlayerLeaves = _playerLeaves,
            TotalPlayerConditionChanges = _playerConditionChanges,
            TotalPlayerStatusChanges = _playerStatusChanges,
            TotalPlayerEffectChanges = _playerEffectChanges,
            TotalPlayerInventoryChanges = _playerInventoryChanges,
            TotalPlayerRoomTransitions = _playerRoomTransitions,
            TotalDoorStateChanges = _doorChanges,
            TotalDoorFlagChanges = _doorFlagChanges,
            TotalDoorDamageEvents = _doorDamageEvents,
            TotalItemPickups = _itemPickups,
            TotalItemDrops = _itemDrops,
            TotalItemQuantityChanges = _itemQuantityChanges,
            TotalScenarioStatusChanges = _scenarioStatusChanges,
            Duration = duration,
            EnemyDamageContributedByPlayer = _damageByPlayer,
            KillsContributedByPlayer = _killsByPlayer,
        };

    private static void AccumulateWeightedDamage(
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players,
        ushort totalDamage,
        Dictionary<string, int> accumulator
    )
    {
        if (players.Count == 0)
            return;

        float totalPower = 0f;
        foreach ((_, _, float power) in players)
            totalPower += power;

        // No player has positive Power (e.g. all are downed/loading) — treat the event as
        // unattributed so that scripted or environmental damage does not inflate player stats.
        if (totalPower <= 0f)
            return;

        foreach ((_, string name, float power) in players)
        {
            if (power <= 0f)
                continue;

            float share = power / totalPower;
            int credited = (int)MathF.Round(totalDamage * share);
            accumulator[name] = accumulator.GetValueOrDefault(name) + credited;
        }
    }

    private static void AccumulateWeightedKill(
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players,
        Dictionary<string, int> accumulator
    )
    {
        foreach ((_, string name, _) in players)
            accumulator[name] = accumulator.GetValueOrDefault(name) + 1;
    }
}

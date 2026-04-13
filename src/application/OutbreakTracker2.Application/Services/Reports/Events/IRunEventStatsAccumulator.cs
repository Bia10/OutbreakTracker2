namespace OutbreakTracker2.Application.Services.Reports.Events;

internal interface IRunEventStatsAccumulator
{
    void Accumulate(EnemySpawnedEvent evt);
    void Accumulate(EnemyKilledEvent evt);
    void Accumulate(EnemyDamagedEvent evt);
    void Accumulate(EnemyDespawnedEvent evt);
    void Accumulate(EnemyStatusChangedEvent evt);
    void Accumulate(EnemyRoomChangedEvent evt);
    void Accumulate(PlayerHealthChangedEvent evt);
    void Accumulate(PlayerVirusChangedEvent evt);
    void Accumulate(PlayerJoinedEvent evt);
    void Accumulate(PlayerLeftEvent evt);
    void Accumulate(PlayerConditionChangedEvent evt);
    void Accumulate(PlayerStatusChangedEvent evt);
    void Accumulate(PlayerEffectChangedEvent evt);
    void Accumulate(PlayerInventoryChangedEvent evt);
    void Accumulate(PlayerRoomChangedEvent evt);
    void Accumulate(DoorStateChangedEvent evt);
    void Accumulate(DoorFlagChangedEvent evt);
    void Accumulate(DoorDamagedEvent evt);
    void Accumulate(ItemPickedUpEvent evt);
    void Accumulate(ItemDroppedEvent evt);
    void Accumulate(ItemQuantityChangedEvent evt);
    void Accumulate(ScenarioStatusChangedEvent evt);
}

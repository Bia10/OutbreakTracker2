using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Utilities;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Reports;

internal sealed class RunReportPlayerDiffProcessor : IRunReportCollectionDiffProcessor<DecodedInGamePlayer>
{
    public void Process(CollectionDiff<DecodedInGamePlayer> diff, RunReportProcessingContext context)
    {
        RunReportProcessingState state = context.State;
        DateTimeOffset now = context.GetCurrentTime();
        bool hadActivePlayers = state.ActivePlayers.Count > 0;
        ScenarioStatus currentScenarioStatus = state.LastScenarioStatus;

        foreach (DecodedInGamePlayer player in diff.Added)
        {
            if (player.IsEnabled)
                state.AllEnabledPlayersBySlot[player.SlotIndex] = player;

            if (player.IsInGame)
            {
                state.ActivePlayers[player.Id] = player;
                state.ActivePlayersBySlot[player.SlotIndex] = player;
            }
        }

        foreach (EntityChange<DecodedInGamePlayer> change in diff.Changed)
        {
            if (change.Current.IsEnabled)
                state.AllEnabledPlayersBySlot[change.Current.SlotIndex] = change.Current;
            else
                state.AllEnabledPlayersBySlot[change.Current.SlotIndex] = null;

            if (change.Current.IsInGame)
            {
                state.ActivePlayers[change.Current.Id] = change.Current;
                state.ActivePlayersBySlot[change.Current.SlotIndex] = change.Current;
            }
            else
            {
                state.ActivePlayers.TryRemove(change.Current.Id, out _);
                state.ActivePlayersBySlot[change.Current.SlotIndex] = null;
            }
        }

        bool isTransitional = currentScenarioStatus.IsTransitional();
        HashSet<Ulid>? removedActiveIds = null;
        if (!isTransitional)
            foreach (DecodedInGamePlayer player in diff.Removed)
            {
                state.AllEnabledPlayersBySlot[player.SlotIndex] = null;

                if (state.ActivePlayers.TryRemove(player.Id, out _))
                {
                    state.ActivePlayersBySlot[player.SlotIndex] = null;
                    removedActiveIds ??= [];
                    removedActiveIds.Add(player.Id);
                }
            }

        bool hasActivePlayers = state.ActivePlayers.Count > 0;

        if (!hadActivePlayers && hasActivePlayers && currentScenarioStatus == ScenarioStatus.InGame)
            context.AutoStartSession();

        foreach (DecodedInGamePlayer player in diff.Added)
        {
            if (!player.IsInGame)
                continue;

            context.Emit(
                new PlayerJoinedEvent(
                    now,
                    player.Id,
                    player.Name,
                    player.CurHealth,
                    player.MaxHealth,
                    player.VirusPercentage
                )
            );
        }

        foreach (EntityChange<DecodedInGamePlayer> change in diff.Changed)
        {
            DecodedInGamePlayer prev = change.Previous;
            DecodedInGamePlayer curr = change.Current;

            if (prev.CurHealth != curr.CurHealth)
                context.Emit(
                    new PlayerHealthChangedEvent(
                        now,
                        curr.Id,
                        curr.Name,
                        prev.CurHealth,
                        curr.CurHealth,
                        curr.MaxHealth
                    )
                );

            if (!string.Equals(prev.Condition, curr.Condition, StringComparison.Ordinal))
                context.Emit(new PlayerConditionChangedEvent(now, curr.Id, curr.Name, prev.Condition, curr.Condition));

            if (prev.VirusPercentage != curr.VirusPercentage)
                context.Emit(
                    new PlayerVirusChangedEvent(now, curr.Id, curr.Name, prev.VirusPercentage, curr.VirusPercentage)
                );

            if (!string.Equals(prev.Status, curr.Status, StringComparison.Ordinal))
                context.Emit(new PlayerStatusChangedEvent(now, curr.Id, curr.Name, prev.Status, curr.Status));

            if (curr.IsInGame && prev.RoomId != curr.RoomId)
                context.Emit(new PlayerRoomChangedEvent(now, curr.Id, curr.Name, prev.RoomId, curr.RoomId));

            EmitEffectChange(context, now, curr, "Bleed", prev.BleedTime, curr.BleedTime);
            EmitEffectChange(context, now, curr, "Herb", prev.HerbTime, curr.HerbTime);
            EmitEffectChange(context, now, curr, "AntiVirus", prev.AntiVirusTime, curr.AntiVirusTime);
            EmitEffectChange(context, now, curr, "AntiVirusG", prev.AntiVirusGTime, curr.AntiVirusGTime);

            EmitInventoryChanges(context, now, curr, prev.Inventory, curr.Inventory, InventoryKind.Main);
            EmitInventoryChanges(
                context,
                now,
                curr,
                prev.SpecialInventory,
                curr.SpecialInventory,
                InventoryKind.Special
            );
            EmitInventoryChanges(context, now, curr, prev.DeadInventory, curr.DeadInventory, InventoryKind.Dead);
            EmitInventoryChanges(
                context,
                now,
                curr,
                prev.SpecialDeadInventory,
                curr.SpecialDeadInventory,
                InventoryKind.SpecialDead
            );
        }

        if (removedActiveIds is not null)
            foreach (DecodedInGamePlayer player in diff.Removed)
                if (removedActiveIds.Contains(player.Id))
                    context.Emit(
                        new PlayerLeftEvent(now, player.Id, player.Name, player.CurHealth, player.VirusPercentage)
                    );

        if (hadActivePlayers && !hasActivePlayers && !isTransitional)
            context.AutoStopSession();
    }

    private static void EmitEffectChange(
        RunReportProcessingContext context,
        DateTimeOffset now,
        DecodedInGamePlayer player,
        string effectName,
        ushort prevTime,
        ushort currTime
    )
    {
        if (prevTime == 0 && currTime > 0)
            context.Emit(new PlayerEffectChangedEvent(now, player.Id, player.Name, effectName, IsApplied: true));
        else if (prevTime > 0 && currTime == 0)
            context.Emit(new PlayerEffectChangedEvent(now, player.Id, player.Name, effectName, IsApplied: false));
    }

    private static void EmitInventoryChanges(
        RunReportProcessingContext context,
        DateTimeOffset now,
        DecodedInGamePlayer player,
        InventorySnapshot prev,
        InventorySnapshot curr,
        InventoryKind kind
    )
    {
        DecodedItem[] scenarioItems = context.State.LastScenario?.Items ?? [];

        for (int i = 0; i < InventorySnapshot.SlotCount; i++)
        {
            if (prev[i] == curr[i])
                continue;

            ResolvedInventorySlotValue oldItem = InventorySlotValueResolver.Resolve(prev[i], scenarioItems);
            ResolvedInventorySlotValue newItem = InventorySlotValueResolver.Resolve(curr[i], scenarioItems);
            context.Emit(
                new PlayerInventoryChangedEvent(
                    now,
                    player.Id,
                    player.Name,
                    kind,
                    i,
                    oldItem.ItemId,
                    oldItem.Name,
                    newItem.ItemId,
                    newItem.Name
                )
            );
        }
    }
}

using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Reports;

internal sealed class RunReportEnemyDiffProcessor : IRunReportCollectionDiffProcessor<DecodedEnemy>
{
    public void Process(CollectionDiff<DecodedEnemy> diff, RunReportProcessingContext context)
    {
        if (context.State.LastScenarioStatus != ScenarioStatus.InGame)
            return;

        DateTimeOffset now = context.GetCurrentTime();

        foreach (DecodedEnemy enemy in diff.Added)
            context.Emit(new EnemySpawnedEvent(now, enemy.Id, enemy.Name, enemy.SlotId, enemy.RoomId, enemy.MaxHp));

        foreach (DecodedEnemy enemy in diff.Removed)
        {
            if (enemy.CurHp <= 1 && !AlertRuleHelpers.IsInvulnerableEnemy(enemy.NameId, enemy.MaxHp))
                context.Emit(
                    new EnemyKilledEvent(
                        now,
                        enemy.Id,
                        enemy.Name,
                        enemy.SlotId,
                        enemy.RoomId,
                        context.FindContributingPlayers(enemy.RoomId)
                    )
                );
            else
                context.Emit(
                    new EnemyDespawnedEvent(
                        now,
                        enemy.Id,
                        enemy.Name,
                        enemy.SlotId,
                        enemy.RoomId,
                        enemy.CurHp,
                        enemy.MaxHp
                    )
                );
        }

        foreach (EntityChange<DecodedEnemy> change in diff.Changed)
        {
            DecodedEnemy prev = change.Previous;
            DecodedEnemy curr = change.Current;

            bool prevAliveHp = prev.CurHp > 0 && prev.CurHp < 0x8000;
            bool currDeadHp = curr.CurHp == 0 || curr.CurHp >= 0x8000;
            if (
                prevAliveHp
                && currDeadHp
                && prev.Enabled != 0
                && curr.NameId != 0
                && prev.NameId == curr.NameId
                && !AlertRuleHelpers.IsInvulnerableEnemy(prev.NameId, prev.MaxHp)
            )
            {
                context.Emit(
                    new EnemyKilledEvent(
                        now,
                        prev.Id,
                        prev.Name,
                        prev.SlotId,
                        prev.RoomId,
                        context.FindContributingPlayers(prev.RoomId)
                    )
                );
                continue;
            }

            if (prev.Enabled != 0 && curr.Enabled == 0 && curr.CurHp > 0)
            {
                context.Emit(
                    new EnemyDespawnedEvent(now, prev.Id, prev.Name, prev.SlotId, prev.RoomId, prev.CurHp, prev.MaxHp)
                );
                continue;
            }

            if (prev.CurHp > curr.CurHp)
                context.Emit(
                    new EnemyDamagedEvent(
                        now,
                        curr.Id,
                        curr.Name,
                        curr.SlotId,
                        curr.RoomId,
                        prev.CurHp,
                        curr.CurHp,
                        curr.MaxHp,
                        context.FindContributingPlayers(curr.RoomId)
                    )
                );

            if (prev.Status != curr.Status)
                context.Emit(
                    new EnemyStatusChangedEvent(
                        now,
                        curr.Id,
                        curr.Name,
                        curr.SlotId,
                        curr.RoomId,
                        prev.Status,
                        curr.Status,
                        context.FindContributingPlayers(curr.RoomId)
                    )
                );

            if (curr.Enabled != 0 && prev.RoomId != curr.RoomId)
                context.Emit(new EnemyRoomChangedEvent(now, curr.Id, curr.Name, curr.SlotId, prev.RoomId, curr.RoomId));
        }
    }
}

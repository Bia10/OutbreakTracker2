using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Reports;

internal sealed class RunReportEnemyDiffProcessor : IRunReportCollectionDiffProcessor<DecodedEnemy>
{
    private const ushort MaxReportableEnemyHp = 30000;
    private const ushort MaxReportableEnemyDamage = 9999;

    private readonly ILogger _logger;

    public RunReportEnemyDiffProcessor(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public void Process(CollectionDiff<DecodedEnemy> diff, RunReportProcessingContext context)
    {
        if (context.State.LastScenarioStatus != ScenarioStatus.InGame)
            return;

        DateTimeOffset now = context.GetCurrentTime();

        foreach (DecodedEnemy enemy in diff.Added)
        {
            if (!TryValidateSpawn(enemy))
                continue;

            context.Emit(new EnemySpawnedEvent(now, enemy.Id, enemy.Name, enemy.SlotId, enemy.RoomId, enemy.MaxHp));
        }

        foreach (DecodedEnemy enemy in diff.Removed)
        {
            if (enemy.CurHp <= 1 && !AlertRuleHelpers.IsInvulnerableEnemy(enemy.NameId, enemy.MaxHp))
            {
                if (!IsReportableMaxHp(enemy.MaxHp))
                {
                    LogFaultyEvent("kill", "enemy max HP is outside the supported report range", enemy, current: null);
                    continue;
                }

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
            }
            else
            {
                if (!IsReportableAliveHp(enemy.CurHp, enemy.MaxHp))
                {
                    LogFaultyEvent(
                        "despawn",
                        "enemy HP snapshot is outside the supported report range",
                        enemy,
                        current: null
                    );
                    continue;
                }

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
        }

        foreach (EntityChange<DecodedEnemy> change in diff.Changed)
        {
            DecodedEnemy prev = change.Previous;
            DecodedEnemy curr = change.Current;

            bool prevAliveHp = IsReportableAliveHp(prev.CurHp, prev.MaxHp);
            bool currDeadHp = IsDeadHp(curr.CurHp);
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
                if (!IsReportableAliveHp(prev.CurHp, prev.MaxHp))
                {
                    LogFaultyEvent("despawn", "enemy HP snapshot is outside the supported report range", prev, curr);
                    continue;
                }

                context.Emit(
                    new EnemyDespawnedEvent(now, prev.Id, prev.Name, prev.SlotId, prev.RoomId, prev.CurHp, prev.MaxHp)
                );
                continue;
            }

            if (prev.CurHp > curr.CurHp)
            {
                if (!TryValidateDamage(prev, curr, out string? reason))
                {
                    LogFaultyEvent("damage", reason, prev, curr);
                }
                else
                {
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
                }
            }

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

    private bool TryValidateSpawn(DecodedEnemy enemy)
    {
        if (IsReportableMaxHp(enemy.MaxHp))
            return true;

        LogFaultyEvent("spawn", "enemy max HP is outside the supported report range", enemy, current: null);
        return false;
    }

    private bool TryValidateDamage(DecodedEnemy prev, DecodedEnemy curr, out string reason)
    {
        if (!IsReportableAliveHp(prev.CurHp, prev.MaxHp))
        {
            reason = "previous enemy HP snapshot is outside the supported report range";
            return false;
        }

        if (!IsReportableAliveHp(curr.CurHp, curr.MaxHp))
        {
            reason = "current enemy HP snapshot is outside the supported report range";
            return false;
        }

        ushort damage = (ushort)(prev.CurHp - curr.CurHp);
        if (damage > MaxReportableEnemyDamage)
        {
            reason = "enemy damage exceeds the supported report range";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private void LogFaultyEvent(string eventKind, string reason, DecodedEnemy previous, DecodedEnemy? current)
    {
        _logger.LogWarning(
            "Excluded faulty enemy run-report {EventKind} event. Reason: {Reason}. Previous: {PreviousEnemy}. Current: {CurrentEnemy}",
            eventKind,
            reason,
            FormatEnemy(previous),
            current is null ? "(none)" : FormatEnemy(current)
        );
    }

    private static bool IsDeadHp(ushort hp) => hp == 0 || hp >= 0x8000;

    private static bool IsReportableAliveHp(ushort currentHp, ushort maxHp) =>
        IsReportableMaxHp(maxHp) && currentHp is > 0 and <= MaxReportableEnemyHp && currentHp <= maxHp;

    private static bool IsReportableMaxHp(ushort maxHp) => maxHp is > 0 and <= MaxReportableEnemyHp;

    private static string FormatEnemy(DecodedEnemy enemy) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"Id={enemy.Id}, Name={enemy.Name}, NameId={enemy.NameId}, Slot={enemy.SlotId}, Room={enemy.RoomId}, Enabled={enemy.Enabled}, CurHp={enemy.CurHp}, MaxHp={enemy.MaxHp}"
        );
}

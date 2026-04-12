using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed class MarkdownRunReportWriter : IRunReportWriter
{
    private readonly ILogger<MarkdownRunReportWriter> _logger;
    private readonly string _outputDirectory;

    public MarkdownRunReportWriter(ILogger<MarkdownRunReportWriter> logger, RunReportOptions options)
    {
        _logger = logger;
        string configuredDirectory = string.IsNullOrWhiteSpace(options.OutputDirectory)
            ? RunReportOptions.DefaultOutputDirectory
            : options.OutputDirectory;
        _outputDirectory = Path.IsPathRooted(configuredDirectory)
            ? configuredDirectory
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredDirectory));

        Directory.CreateDirectory(_outputDirectory);
        _logger.LogInformation("Run reports will be saved to: {ReportsDirectory}", _outputDirectory);
    }

    public async Task WriteAsync(RunReport report, CancellationToken cancellationToken = default)
    {
        string fileName = $"run_{report.SessionId}_{report.StartedAt:yyyyMMdd_HHmmss_UTC}.md";
        string filePath = Path.Combine(_outputDirectory, fileName);
        string content = BuildMarkdown(report);

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Run report written: {FileName} ({EventCount} events, duration {Duration}) → {FilePath}",
            fileName,
            report.Events.Count,
            report.Duration,
            filePath
        );
    }

    private static string BuildMarkdown(RunReport report)
    {
        RunReportStats stats = report.ComputeStats();
        StringBuilder sb = new();

        sb.AppendLine("# Outbreak Tracker 2 — Scenario Run Report");
        sb.AppendLine();
        sb.Append(
                CultureInfo.InvariantCulture,
                $"**Scenario:** {(string.IsNullOrEmpty(report.ScenarioName) ? report.ScenarioId : report.ScenarioName)}  "
            )
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"**Session:** `{report.SessionId}`  ").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"**Started:** {report.StartedAt:yyyy-MM-dd HH:mm:ss} UTC  ")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"**Ended:** {report.EndedAt:yyyy-MM-dd HH:mm:ss} UTC  ").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"**Duration:** {FormatDuration(report.Duration)}  ").AppendLine();
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        sb.AppendLine("## Summary Statistics");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.Append(CultureInfo.InvariantCulture, $"| Enemy Kills | {stats.TotalEnemyKills} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Enemy Despawns (scripted) | {stats.TotalDespawns} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Total Damage Taken | {stats.TotalDamageTaken} HP |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Peak Virus (any player) | {stats.PeakVirusPercentage:F3}% |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Door State Changes | {stats.TotalDoorStateChanges} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Items Picked Up | {stats.TotalItemPickups} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Items Dropped | {stats.TotalItemDrops} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Total Events Recorded | {report.Events.Count} |").AppendLine();
        sb.AppendLine();

        if (stats.EnemyDamageContributedByPlayer.Count > 0 || stats.KillsContributedByPlayer.Count > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Damage Contributions");
            sb.AppendLine();
            sb.AppendLine("| Player | Damage Dealt | Kills |");
            sb.AppendLine("|--------|-------------|-------|");

            HashSet<string> allPlayers =
            [
                .. stats.EnemyDamageContributedByPlayer.Keys,
                .. stats.KillsContributedByPlayer.Keys,
            ];
            foreach (string player in allPlayers)
            {
                int damage = stats.EnemyDamageContributedByPlayer.GetValueOrDefault(player);
                int playerKills = stats.KillsContributedByPlayer.GetValueOrDefault(player);
                sb.Append(CultureInfo.InvariantCulture, $"| {player} | {damage} HP | {playerKills} |").AppendLine();
            }

            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
        AppendMonsterLog(sb, report);
        sb.AppendLine("## Event Timeline");
        sb.AppendLine();
        sb.AppendLine("| Scenario Time | Event |");
        sb.AppendLine("|--------------|-------|");

        Scenario scenario = report.Scenario;
        foreach (RunEvent evt in report.Events)
        {
            string scenarioTime = TimeUtility.GetTimeFromFrames(evt.ScenarioFrame);
            string description = DescribeEvent(evt, scenario);
            sb.Append(CultureInfo.InvariantCulture, $"| `{scenarioTime}` | {description} |").AppendLine();
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private static string DescribeEvent(RunEvent evt, Scenario scenario) =>
        evt switch
        {
            PlayerJoinedEvent e =>
                $"Player **{e.PlayerName}** joined (HP: {e.InitialHealth}/{e.MaxHealth}, Virus: {e.InitialVirusPercentage:F3}%)",

            PlayerLeftEvent e =>
                $"Player **{e.PlayerName}** left (HP: {e.FinalHealth}, Virus: {e.FinalVirusPercentage:F3}%)",

            PlayerHealthChangedEvent { IsDamage: true } e =>
                $"Player **{e.PlayerName}** took **{e.OldHealth - e.NewHealth} damage** ({e.OldHealth} → {e.NewHealth}/{e.MaxHealth})",

            PlayerHealthChangedEvent { IsHeal: true } e =>
                $"Player **{e.PlayerName}** healed **+{e.NewHealth - e.OldHealth} HP** ({e.OldHealth} → {e.NewHealth}/{e.MaxHealth})",

            PlayerConditionChangedEvent e =>
                $"Player **{e.PlayerName}** condition: {e.OldCondition} → **{e.NewCondition}**",

            PlayerVirusChangedEvent { Delta: > 0 } e =>
                $"Player **{e.PlayerName}** virus: {e.OldVirusPercentage:F3}% → **{e.NewVirusPercentage:F3}%** (+{e.Delta:F3}%)",

            PlayerVirusChangedEvent e =>
                $"Player **{e.PlayerName}** virus: {e.OldVirusPercentage:F3}% → **{e.NewVirusPercentage:F3}%** ({e.Delta:F3}%)",

            EnemySpawnedEvent e =>
                $"Enemy **{e.EnemyName}** spawned ({scenario.GetRoomName(e.RoomId)}, Slot {e.SlotId}, HP: {e.MaxHp})",

            EnemyKilledEvent e =>
                $"Enemy **{e.EnemyName}** killed ({scenario.GetRoomName(e.RoomId)}, Slot {e.SlotId}){FormatContributions(e.ContributingPlayers)}",

            EnemyDamagedEvent e =>
                $"Enemy **{e.EnemyName}** damaged: {e.OldHp} → {e.NewHp}/{e.MaxHp} (-{e.Damage}){FormatContributions(e.ContributingPlayers)}",

            EnemyDespawnedEvent e =>
                $"Enemy **{e.EnemyName}** despawned ({scenario.GetRoomName(e.RoomId)}, Slot {e.SlotId}, HP remaining: {e.RemainingHp}/{e.MaxHp})",

            EnemyStatusChangedEvent { IsActivation: true } e =>
                $"Enemy **{e.EnemyName}** activated 0x{e.OldStatus:X2} → 0x{e.NewStatus:X2} ({scenario.GetRoomName(e.RoomId)}){FormatContributions(e.ContributingPlayers)}",

            EnemyStatusChangedEvent e =>
                $"Enemy **{e.EnemyName}** status: 0x{e.OldStatus:X2} → 0x{e.NewStatus:X2} ({scenario.GetRoomName(e.RoomId)}){FormatContributions(e.ContributingPlayers)}",

            DoorStateChangedEvent e => $"Door status changed: {e.OldStatus} → **{e.NewStatus}**",

            DoorDamagedEvent e => $"Door damaged: {e.OldHp} → {e.NewHp} HP (-{e.Damage})",

            DoorFlagChangedEvent e => $"Door flag changed: 0x{e.OldFlag:X4} → **0x{e.NewFlag:X4}**",

            ItemPickedUpEvent e when string.IsNullOrEmpty(e.PickedUpByName) =>
                $"**{e.TypeName}** looted from scenario slot ({scenario.GetRoomName(e.RoomId)})",
            ItemPickedUpEvent e =>
                $"**{e.PickedUpByName}** looted **{e.TypeName}** from scenario slot ({scenario.GetRoomName(e.RoomId)})",

            ItemDroppedEvent e when string.IsNullOrEmpty(e.PreviousHolder) =>
                $"**{e.TypeName}** returned to scenario slot ({scenario.GetRoomName(e.RoomId)})",
            ItemDroppedEvent e =>
                $"**{e.PreviousHolder}** returned **{e.TypeName}** to scenario slot ({scenario.GetRoomName(e.RoomId)})",
            ItemQuantityChangedEvent e =>
                $"**{e.TypeName}** (slot {e.SlotIndex}) quantity: {e.OldQuantity} → **{e.NewQuantity}** ({scenario.GetRoomName(e.RoomId)})",

            ScenarioStatusChangedEvent e => $"Scenario status: {e.OldStatus} → **{e.NewStatus}**",
            PlayerStatusChangedEvent e => $"Player **{e.PlayerName}** status: {e.OldStatus} → **{e.NewStatus}**",

            PlayerEffectChangedEvent { IsApplied: true } e =>
                $"Player **{e.PlayerName}** effect **{e.EffectName}** applied",
            PlayerEffectChangedEvent e => $"Player **{e.PlayerName}** effect **{e.EffectName}** expired",

            PlayerInventoryChangedEvent e =>
                $"Player **{e.PlayerName}** {e.Kind} slot {e.SlotIndex}: {e.OldItemName} → **{e.NewItemName}**",

            PlayerRoomChangedEvent e => $"Player **{e.PlayerName}** moved room: {e.OldRoomId} → **{e.NewRoomId}**",

            EnemyRoomChangedEvent e =>
                $"Enemy **{e.EnemyName}** (Slot {e.SlotId}) moved room: {e.OldRoomId} → **{e.NewRoomId}**",

            _ => evt.GetType().Name,
        };

    private static void AppendMonsterLog(StringBuilder sb, RunReport report)
    {
        // Build per-enemy lifecycle records: spawn time, spawn room, end time, end room, outcome.
        Dictionary<Ulid, (string Name, DateTimeOffset SpawnedAt, byte SpawnRoom, ushort MaxHp)> spawns = [];
        List<(
            string Name,
            DateTimeOffset SpawnedAt,
            byte SpawnRoom,
            ushort MaxHp,
            DateTimeOffset EndedAt,
            byte EndRoom,
            string Outcome
        )> log = [];

        foreach (RunEvent evt in report.Events)
        {
            switch (evt)
            {
                case EnemySpawnedEvent s:
                    spawns[s.EnemyId] = (s.EnemyName, s.OccurredAt, s.RoomId, s.MaxHp);
                    break;
                case EnemyKilledEvent k:
                    if (spawns.TryGetValue(k.EnemyId, out var ks))
                    {
                        log.Add((ks.Name, ks.SpawnedAt, ks.SpawnRoom, ks.MaxHp, k.OccurredAt, k.RoomId, "Killed"));
                        spawns.Remove(k.EnemyId);
                    }
                    else
                    {
                        // Enemy was present before session started — record without spawn time.
                        log.Add((k.EnemyName, report.StartedAt, k.RoomId, 0, k.OccurredAt, k.RoomId, "Killed"));
                    }

                    break;
                case EnemyDespawnedEvent d:
                    if (spawns.TryGetValue(d.EnemyId, out var ds))
                    {
                        log.Add((ds.Name, ds.SpawnedAt, ds.SpawnRoom, ds.MaxHp, d.OccurredAt, d.RoomId, "Despawned"));
                        spawns.Remove(d.EnemyId);
                    }
                    else
                    {
                        log.Add(
                            (d.EnemyName, report.StartedAt, d.RoomId, d.MaxHp, d.OccurredAt, d.RoomId, "Despawned")
                        );
                    }

                    break;
            }
        }

        // Enemies still alive at session end.
        foreach (var (id, info) in spawns)
            log.Add(
                (info.Name, info.SpawnedAt, info.SpawnRoom, info.MaxHp, report.EndedAt, info.SpawnRoom, "Still alive")
            );

        if (log.Count == 0)
            return;

        sb.AppendLine("## Monster Log");
        sb.AppendLine();
        sb.AppendLine("| Enemy | Spawn Room | Spawn Time | End Room | End Time | Duration | Outcome |");
        sb.AppendLine("|-------|-----------|------------|---------|---------|----------|---------|");

        foreach (var (name, spawnedAt, spawnRoom, _, endedAt, endRoom, outcome) in log)
        {
            TimeSpan alive = endedAt - spawnedAt;
            string spawnElapsed = FormatElapsed(spawnedAt - report.StartedAt);
            string endElapsed = FormatElapsed(endedAt - report.StartedAt);
            sb.Append(
                    CultureInfo.InvariantCulture,
                    $"| {name} | {spawnRoom} | `{spawnElapsed}` | {endRoom} | `{endElapsed}` | {FormatDuration(alive)} | {outcome} |"
                )
                .AppendLine();
        }

        sb.AppendLine();
    }

    private static string FormatContributions(IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players)
    {
        if (players.Count == 0)
            return string.Empty;

        StringBuilder sb = new(" — by ");
        for (int i = 0; i < players.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append("**").Append(players[i].PlayerName).Append("**");
        }

        return sb.ToString();
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return $"+{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

        return $"+{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";

        return $"{duration.Minutes}m {duration.Seconds}s";
    }
}

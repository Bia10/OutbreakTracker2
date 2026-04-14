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
        _outputDirectory = RunReportOutputPathUtility.ResolveOutputDirectory(options);

        _logger.LogInformation("Markdown run reports will be saved to: {ReportsDirectory}", _outputDirectory);
    }

    public async Task WriteAsync(RunReport report, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_outputDirectory);

        string fileName = RunReportOutputPathUtility.GetFileName(report, "md");
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

        BuildHeaderSection(sb, report);
        BuildStatsSection(sb, stats, report.Events.Count);
        BuildDamageSection(sb, stats);
        AppendSectionSeparator(sb);
        AppendMonsterLog(sb, report, report.Scenario);
        BuildTimelineSection(sb, report);

        return sb.ToString();
    }

    private static void BuildHeaderSection(StringBuilder sb, RunReport report)
    {
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
        sb.Append(
                CultureInfo.InvariantCulture,
                $"**Duration:** {RunReportFormatting.FormatDuration(report.Duration)}  "
            )
            .AppendLine();
        sb.AppendLine();

        AppendSectionSeparator(sb);
    }

    private static void BuildStatsSection(StringBuilder sb, RunReportStats stats, int eventCount)
    {
        sb.AppendLine("## Summary Statistics");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.Append(CultureInfo.InvariantCulture, $"| Enemy Spawns | {stats.TotalEnemySpawns} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Enemy Kills | {stats.TotalEnemyKills} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Enemy Despawns (scripted) | {stats.TotalDespawns} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Enemy Damage Events | {stats.TotalEnemyDamageEvents} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Enemy Status Changes | {stats.TotalEnemyStatusChanges} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Enemy Activations | {stats.TotalEnemyActivations} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Enemy Room Transitions | {stats.TotalEnemyRoomTransitions} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Joins | {stats.TotalPlayerJoins} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Leaves | {stats.TotalPlayerLeaves} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Health Changes | {stats.TotalPlayerHealthChanges} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Total Damage Taken (all players) | {stats.TotalDamageTaken} HP |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Healing Events | {stats.TotalPlayerHealingEvents} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Virus Changes | {stats.TotalPlayerVirusChanges} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Peak Virus (any player) | {stats.PeakVirusPercentage:F3}% |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Status Changes | {stats.TotalPlayerStatusChanges} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Condition Changes | {stats.TotalPlayerConditionChanges} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Effect Changes | {stats.TotalPlayerEffectChanges} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Inventory Changes | {stats.TotalPlayerInventoryChanges} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Player Room Transitions | {stats.TotalPlayerRoomTransitions} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Door State Changes | {stats.TotalDoorStateChanges} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Door Flag Changes | {stats.TotalDoorFlagChanges} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Door Damage Events | {stats.TotalDoorDamageEvents} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Items Picked Up | {stats.TotalItemPickups} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Items Dropped | {stats.TotalItemDrops} |").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Item Quantity Changes | {stats.TotalItemQuantityChanges} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Scenario Status Changes | {stats.TotalScenarioStatusChanges} |")
            .AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"| Total Events Recorded | {eventCount} |").AppendLine();
        sb.AppendLine();
    }

    private static void BuildDamageSection(StringBuilder sb, RunReportStats stats)
    {
        if (stats.EnemyDamageContributedByPlayer.Count == 0 && stats.KillsContributedByPlayer.Count == 0)
            return;

        AppendSectionSeparator(sb);
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

    private static void BuildTimelineSection(StringBuilder sb, RunReport report)
    {
        sb.AppendLine("## Event Timeline");
        sb.AppendLine();
        sb.AppendLine("| Scenario Time | Event |");
        sb.AppendLine("|--------------|-------|");

        foreach (RunEvent evt in report.Events)
        {
            string scenarioTime = RunReportFormatting.FormatScenarioTime(evt.ScenarioFrame);
            sb.Append(CultureInfo.InvariantCulture, $"| `{scenarioTime}` | {evt.Describe(report.Scenario)} |")
                .AppendLine();
        }

        sb.AppendLine();
    }

    private static void AppendSectionSeparator(StringBuilder sb)
    {
        sb.AppendLine("---");
        sb.AppendLine();
    }

    private static void AppendMonsterLog(StringBuilder sb, RunReport report, Scenario scenario)
    {
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
                case EnemySpawnedEvent spawned:
                    spawns[spawned.EnemyId] = (spawned.EnemyName, spawned.OccurredAt, spawned.RoomId, spawned.MaxHp);
                    break;
                case EnemyKilledEvent killed:
                    if (spawns.TryGetValue(killed.EnemyId, out var killedSpawn))
                    {
                        log.Add(
                            (
                                killedSpawn.Name,
                                killedSpawn.SpawnedAt,
                                killedSpawn.SpawnRoom,
                                killedSpawn.MaxHp,
                                killed.OccurredAt,
                                killed.RoomId,
                                "Killed"
                            )
                        );
                        spawns.Remove(killed.EnemyId);
                    }
                    else
                    {
                        log.Add(
                            (
                                killed.EnemyName,
                                report.StartedAt,
                                killed.RoomId,
                                0,
                                killed.OccurredAt,
                                killed.RoomId,
                                "Killed"
                            )
                        );
                    }

                    break;
                case EnemyDespawnedEvent despawned:
                    if (spawns.TryGetValue(despawned.EnemyId, out var despawnedSpawn))
                    {
                        log.Add(
                            (
                                despawnedSpawn.Name,
                                despawnedSpawn.SpawnedAt,
                                despawnedSpawn.SpawnRoom,
                                despawnedSpawn.MaxHp,
                                despawned.OccurredAt,
                                despawned.RoomId,
                                "Despawned"
                            )
                        );
                        spawns.Remove(despawned.EnemyId);
                    }
                    else
                    {
                        log.Add(
                            (
                                despawned.EnemyName,
                                report.StartedAt,
                                despawned.RoomId,
                                despawned.MaxHp,
                                despawned.OccurredAt,
                                despawned.RoomId,
                                "Despawned"
                            )
                        );
                    }

                    break;
            }
        }

        foreach (var (_, info) in spawns)
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
            string spawnElapsed = RunReportFormatting.FormatElapsed(spawnedAt - report.StartedAt);
            string endElapsed = RunReportFormatting.FormatElapsed(endedAt - report.StartedAt);
            sb.Append(
                    CultureInfo.InvariantCulture,
                    $"| {name} | {scenario.GetRoomName(spawnRoom)} | `{spawnElapsed}` | {scenario.GetRoomName(endRoom)} | `{endElapsed}` | {RunReportFormatting.FormatDuration(alive)} | {outcome} |"
                )
                .AppendLine();
        }

        sb.AppendLine();
    }
}

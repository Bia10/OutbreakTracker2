using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed class HtmlRunReportWriter : IRunReportWriter
{
    private const double RingCircumference = 339.29200658769764;

    private readonly ILogger<HtmlRunReportWriter> _logger;
    private readonly string _outputDirectory;

    public HtmlRunReportWriter(ILogger<HtmlRunReportWriter> logger, RunReportOptions options)
    {
        _logger = logger;
        _outputDirectory = RunReportOutputPathUtility.ResolveOutputDirectory(options);

        _logger.LogInformation("HTML run reports will be saved to: {ReportsDirectory}", _outputDirectory);
    }

    public async Task WriteAsync(RunReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        Directory.CreateDirectory(_outputDirectory);

        string fileName = RunReportOutputPathUtility.GetFileName(report, "html");
        string filePath = Path.Combine(_outputDirectory, fileName);
        string content = BuildHtml(report);

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "HTML run report written: {FileName} ({EventCount} events, duration {Duration}) -> {FilePath}",
            fileName,
            report.Events.Count,
            report.Duration,
            filePath
        );
    }

    private static string BuildHtml(RunReport report)
    {
        RunReportStats stats = report.ComputeStats();
        string scenarioTitle = GetScenarioTitle(report);
        List<HtmlEventEntry> events = BuildEventEntries(report);
        Dictionary<string, int> categoryCounts = BuildCategoryCounts(events);
        HtmlPayload payload = new(
            scenarioTitle,
            report.SessionId.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            RunReportFormatting.FormatDuration(report.Duration),
            Math.Max(1L, (long)report.Duration.TotalMilliseconds),
            report.Events.Count,
            events
        );

        StringBuilder builder = new(capacity: 64 * 1024);
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"en\" data-theme=\"dark\">");
        AppendHead(builder, scenarioTitle);
        builder.AppendLine("<body>");
        AppendStickyBar(builder, scenarioTitle, report.Events.Count, report.Duration);
        builder.AppendLine("<div class=\"page-shell\">");
        AppendHeader(builder, report, scenarioTitle);
        AppendHero(builder, report, stats, events, categoryCounts);
        AppendInsights(builder, report, stats, categoryCounts);
        AppendToolbar(builder);
        builder.AppendLine("<section id=\"emptyState\" class=\"empty-state\" hidden>");
        builder.AppendLine("<h2>No matching events</h2>");
        builder.AppendLine("<p>Adjust the search, category filter, or grouping to bring events back into view.</p>");
        builder.AppendLine("</section>");
        builder.AppendLine("<div id=\"eventGroups\" class=\"group-list\"></div>");
        builder.AppendLine("</div>");
        builder.Append("<script id=\"report-data\" type=\"application/json\">");
        builder.Append(JsonSerializer.Serialize(payload, typeof(HtmlPayload), HtmlRunReportJsonContext.Default));
        builder.AppendLine("</script>");
        builder.AppendLine("<script>");
        builder.AppendLine(ClientScript);
        builder.AppendLine("</script>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static void AppendHead(StringBuilder builder, string scenarioTitle)
    {
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"UTF-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        builder.AppendLine("<meta name=\"color-scheme\" content=\"dark light\">");
        builder.Append("<title>");
        AppendEncoded(builder, scenarioTitle);
        builder.AppendLine(" - Scenario Run Report</title>");
        builder.AppendLine("<style>");
        builder.AppendLine(Styles);
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
    }

    private static void AppendStickyBar(StringBuilder builder, string scenarioTitle, int totalEvents, TimeSpan duration)
    {
        builder.AppendLine("<div class=\"sticky-bar\" id=\"stickyBar\" aria-hidden=\"true\">");
        builder.AppendLine("<div class=\"sticky-copy\">");
        builder.Append("<span class=\"sticky-name\">");
        AppendEncoded(builder, scenarioTitle);
        builder.AppendLine("</span>");
        builder.AppendLine("<div class=\"sticky-metrics\">");
        builder.Append("<span class=\"sticky-pill\">");
        AppendEncoded(builder, string.Create(CultureInfo.InvariantCulture, $"{totalEvents} events"));
        builder.AppendLine("</span>");
        builder.Append("<span class=\"sticky-pill\">");
        AppendEncoded(builder, RunReportFormatting.FormatDuration(duration));
        builder.AppendLine("</span>");
        builder.AppendLine("</div>");
        builder.AppendLine("</div>");
        builder.AppendLine(
            "<button class=\"sticky-search\" id=\"stickySearchBtn\" type=\"button\" aria-label=\"Focus event search\">Search</button>"
        );
        builder.AppendLine("</div>");
    }

    private static void AppendHeader(StringBuilder builder, RunReport report, string scenarioTitle)
    {
        builder.AppendLine("<header class=\"page-header\">");
        builder.AppendLine("<div class=\"header-copy\">");
        builder.AppendLine("<span class=\"eyebrow\">Scenario Run Report</span>");
        builder.Append("<h1>");
        AppendEncoded(builder, scenarioTitle);
        builder.AppendLine("</h1>");
        builder.AppendLine(
            "<p class=\"subtitle\">Interactive export for reviewing combat, item flow, and scenario state changes.</p>"
        );
        builder.AppendLine("<div class=\"meta-row\">");
        AppendMetaChip(
            builder,
            "Session",
            report.SessionId.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
        );
        AppendMetaChip(
            builder,
            "Scenario Id",
            string.IsNullOrWhiteSpace(report.ScenarioId) ? "Unknown" : report.ScenarioId
        );
        AppendMetaChip(builder, "Started", RunReportFormatting.FormatUtc(report.StartedAt));
        AppendMetaChip(builder, "Ended", RunReportFormatting.FormatUtc(report.EndedAt));
        AppendMetaChip(builder, "Exported", RunReportFormatting.FormatUtc(DateTimeOffset.UtcNow));
        builder.AppendLine("</div>");
        builder.AppendLine("</div>");
        builder.AppendLine(
            "<button id=\"themeToggle\" class=\"theme-toggle\" type=\"button\" aria-label=\"Toggle report theme\">Theme</button>"
        );
        builder.AppendLine("</header>");
    }

    private static void AppendHero(
        StringBuilder builder,
        RunReport report,
        RunReportStats stats,
        IReadOnlyList<HtmlEventEntry> events,
        IReadOnlyDictionary<string, int> categoryCounts
    )
    {
        int playersSeen = CountDistinctPlayers(report.Events);
        int distinctEventTypes = events
            .Select(static entry => entry.TypeLabel)
            .Distinct(StringComparer.Ordinal)
            .Count();

        builder.AppendLine("<section class=\"hero-grid\">");
        builder.AppendLine("<div class=\"hero-panel ring-panel\">");
        builder.AppendLine("<div class=\"ring-shell\">");
        builder.AppendLine("<svg class=\"hero-ring\" viewBox=\"0 0 120 120\" aria-hidden=\"true\">");
        builder.AppendLine(
            "<circle cx=\"60\" cy=\"60\" r=\"54\" fill=\"none\" stroke=\"var(--surface-2)\" stroke-width=\"10\"></circle>"
        );
        AppendRingSegments(builder, categoryCounts, report.Events.Count);
        builder.AppendLine("</svg>");
        builder.AppendLine("<div class=\"ring-center\">");
        builder.Append("<span class=\"ring-value\">");
        AppendEncoded(builder, string.Create(CultureInfo.InvariantCulture, $"{report.Events.Count}"));
        builder.AppendLine("</span>");
        builder.AppendLine("<span class=\"ring-label\">tracked events</span>");
        builder.AppendLine("</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"mix-legend\">");

        foreach (string category in CategoryOrder)
        {
            int count = categoryCounts.GetValueOrDefault(category);
            if (count == 0)
                continue;

            builder.Append("<div class=\"legend-row\"><span class=\"legend-dot ");
            AppendEncoded(builder, category);
            builder.Append("\"></span><span class=\"legend-name\">");
            AppendEncoded(builder, GetCategoryLabel(category));
            builder.Append("</span><span class=\"legend-value\">");
            AppendEncoded(builder, string.Create(CultureInfo.InvariantCulture, $"{count}"));
            builder.AppendLine("</span></div>");
        }

        builder.AppendLine("</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"hero-panel stats-panel\">");
        builder.AppendLine("<div class=\"stats-grid\">");
        AppendStatCard(builder, "Duration", RunReportFormatting.FormatDuration(report.Duration), "neutral");
        AppendStatCard(
            builder,
            "Players Seen",
            string.Create(CultureInfo.InvariantCulture, $"{playersSeen}"),
            "player"
        );
        AppendStatCard(
            builder,
            "Enemy Kills",
            string.Create(CultureInfo.InvariantCulture, $"{stats.TotalEnemyKills}"),
            "enemy"
        );
        AppendStatCard(
            builder,
            "Damage Taken",
            string.Create(CultureInfo.InvariantCulture, $"{stats.TotalDamageTaken} HP"),
            "warn"
        );
        AppendStatCard(
            builder,
            "Peak Virus",
            string.Create(CultureInfo.InvariantCulture, $"{stats.PeakVirusPercentage:F3}%"),
            "warn"
        );
        AppendStatCard(
            builder,
            "Event Types",
            string.Create(CultureInfo.InvariantCulture, $"{distinctEventTypes}"),
            "accent"
        );
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"histogram-block\">");
        builder.AppendLine("<div class=\"panel-kicker\">Event density</div>");
        builder.AppendLine("<div class=\"histogram\" id=\"eventHistogram\"></div>");
        builder.AppendLine("</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
    }

    private static void AppendInsights(
        StringBuilder builder,
        RunReport report,
        RunReportStats stats,
        IReadOnlyDictionary<string, int> categoryCounts
    )
    {
        builder.AppendLine("<section class=\"insight-grid\">");
        AppendRankingPanel(
            builder,
            "Damage leaders",
            "Damage dealt to tracked enemies.",
            stats.EnemyDamageContributedByPlayer,
            " HP"
        );
        AppendRankingPanel(
            builder,
            "Kill leaders",
            "Players present for enemy takedowns.",
            stats.KillsContributedByPlayer,
            string.Empty
        );
        AppendEventMixPanel(builder, report.Events.Count, categoryCounts);
        builder.AppendLine("</section>");
    }

    private static void AppendRankingPanel(
        StringBuilder builder,
        string title,
        string subtitle,
        IReadOnlyDictionary<string, int> values,
        string valueSuffix
    )
    {
        builder.AppendLine("<section class=\"insight-panel\">");
        builder.Append("<h2>");
        AppendEncoded(builder, title);
        builder.AppendLine("</h2>");
        builder.Append("<p class=\"panel-note\">");
        AppendEncoded(builder, subtitle);
        builder.AppendLine("</p>");

        if (values.Count == 0)
        {
            builder.AppendLine("<p class=\"panel-empty\">No contribution data was recorded for this run.</p>");
        }
        else
        {
            builder.AppendLine("<div class=\"ranking-list\">");
            foreach (
                (string name, int value) in values
                    .OrderByDescending(static pair => pair.Value)
                    .ThenBy(static pair => pair.Key)
                    .Take(5)
            )
            {
                builder.AppendLine("<div class=\"ranking-row\">");
                builder.Append("<span class=\"ranking-name\">");
                AppendEncoded(builder, name);
                builder.Append("</span><span class=\"ranking-value\">");
                AppendEncoded(builder, string.Create(CultureInfo.InvariantCulture, $"{value}{valueSuffix}"));
                builder.AppendLine("</span></div>");
            }
            builder.AppendLine("</div>");
        }

        builder.AppendLine("</section>");
    }

    private static void AppendEventMixPanel(
        StringBuilder builder,
        int totalEvents,
        IReadOnlyDictionary<string, int> categoryCounts
    )
    {
        builder.AppendLine("<section class=\"insight-panel\">");
        builder.AppendLine("<h2>Event mix</h2>");
        builder.AppendLine(
            "<p class=\"panel-note\">How the run splits across player, enemy, door, item, and scenario activity.</p>"
        );
        builder.AppendLine("<div class=\"ranking-list\">");

        foreach (string category in CategoryOrder)
        {
            int count = categoryCounts.GetValueOrDefault(category);
            if (count == 0)
                continue;

            double percentage = totalEvents == 0 ? 0.0 : (double)count / totalEvents * 100.0;
            builder.AppendLine("<div class=\"ranking-row\">");
            builder.Append("<span class=\"ranking-name\">");
            AppendEncoded(builder, GetCategoryLabel(category));
            builder.Append("</span><span class=\"ranking-value\">");
            AppendEncoded(builder, string.Create(CultureInfo.InvariantCulture, $"{count} ({percentage:F1}%)"));
            builder.AppendLine("</span></div>");
        }

        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
    }

    private static void AppendToolbar(StringBuilder builder)
    {
        builder.AppendLine("<section class=\"toolbar\">");
        builder.AppendLine("<div class=\"search-box\">");
        builder.AppendLine("<label class=\"search-label\" for=\"eventSearch\">Search</label>");
        builder.AppendLine(
            "<input id=\"eventSearch\" type=\"search\" autocomplete=\"off\" spellcheck=\"false\" placeholder=\"Search events, actors, rooms, or IDs\">"
        );
        builder.AppendLine("<button id=\"clearSearch\" class=\"toolbar-btn subtle\" type=\"button\">Clear</button>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"toolbar-section\">");
        builder.AppendLine("<span class=\"toolbar-label\">Category</span>");
        builder.AppendLine(
            "<div id=\"categoryFilters\" class=\"filter-row\" role=\"group\" aria-label=\"Filter events by category\"></div>"
        );
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"toolbar-section compact\">");
        builder.AppendLine("<span class=\"toolbar-label\">Group by</span>");
        builder.AppendLine("<div class=\"toggle-row\" role=\"radiogroup\" aria-label=\"Group events\">");
        builder.AppendLine(
            "<button class=\"toggle-btn active\" type=\"button\" data-group=\"category\" aria-pressed=\"true\">Category</button>"
        );
        builder.AppendLine(
            "<button class=\"toggle-btn\" type=\"button\" data-group=\"type\" aria-pressed=\"false\">Event type</button>"
        );
        builder.AppendLine(
            "<button class=\"toggle-btn\" type=\"button\" data-group=\"actor\" aria-pressed=\"false\">Actor</button>"
        );
        builder.AppendLine("</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"toolbar-section compact\">");
        builder.AppendLine("<span class=\"toolbar-label\">Sort</span>");
        builder.AppendLine("<div class=\"toggle-row\" role=\"radiogroup\" aria-label=\"Sort events\">");
        builder.AppendLine(
            "<button class=\"toggle-btn active\" type=\"button\" data-sort=\"timeline\" aria-pressed=\"true\">Timeline</button>"
        );
        builder.AppendLine(
            "<button class=\"toggle-btn\" type=\"button\" data-sort=\"newest\" aria-pressed=\"false\">Newest</button>"
        );
        builder.AppendLine(
            "<button class=\"toggle-btn\" type=\"button\" data-sort=\"name\" aria-pressed=\"false\">Name</button>"
        );
        builder.AppendLine("</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"toolbar-actions\">");
        builder.AppendLine("<button id=\"expandAll\" class=\"toolbar-btn\" type=\"button\">Expand all</button>");
        builder.AppendLine("<button id=\"collapseAll\" class=\"toolbar-btn\" type=\"button\">Collapse all</button>");
        builder.AppendLine("<span id=\"filterSummary\" class=\"filter-summary\" aria-live=\"polite\"></span>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
    }

    private static void AppendMetaChip(StringBuilder builder, string label, string value)
    {
        builder.Append("<span class=\"meta-chip\"><span class=\"meta-key\">");
        AppendEncoded(builder, label);
        builder.Append("</span><span class=\"meta-value\">");
        AppendEncoded(builder, value);
        builder.AppendLine("</span></span>");
    }

    private static void AppendStatCard(StringBuilder builder, string label, string value, string tone)
    {
        builder.Append("<article class=\"stat-card ");
        AppendEncoded(builder, tone);
        builder.Append("\"><span class=\"stat-label\">");
        AppendEncoded(builder, label);
        builder.Append("</span><span class=\"stat-value\">");
        AppendEncoded(builder, value);
        builder.AppendLine("</span></article>");
    }

    private static void AppendRingSegments(
        StringBuilder builder,
        IReadOnlyDictionary<string, int> categoryCounts,
        int totalEvents
    )
    {
        if (totalEvents <= 0)
            return;

        double offset = 0.0;
        foreach (string category in CategoryOrder)
        {
            int count = categoryCounts.GetValueOrDefault(category);
            if (count <= 0)
                continue;

            double length = RingCircumference * count / totalEvents;
            builder
                .Append(
                    CultureInfo.InvariantCulture,
                    $"<circle class=\"ring-segment {category}\" cx=\"60\" cy=\"60\" r=\"54\" fill=\"none\" stroke-width=\"10\" stroke-linecap=\"round\" stroke-dasharray=\"{length:F3} {RingCircumference - length:F3}\" stroke-dashoffset=\"-{offset:F3}\" transform=\"rotate(-90 60 60)\"></circle>"
                )
                .AppendLine();
            offset += length;
        }
    }

    private static List<HtmlEventEntry> BuildEventEntries(RunReport report)
    {
        List<HtmlEventEntry> entries = new(report.Events.Count);

        for (int index = 0; index < report.Events.Count; index++)
        {
            RunEvent evt = report.Events[index];
            string category = GetCategory(evt);
            string typeLabel = GetTypeLabel(evt);
            string actorLabel = GetActorLabel(evt);
            string description = evt.Describe(report.Scenario);
            string descriptionText = StripMarkdown(description);
            List<HtmlDetailItem> details = BuildDetails(evt, report.Scenario);
            string searchText = BuildSearchText(actorLabel, typeLabel, descriptionText, details);

            entries.Add(
                new HtmlEventEntry(
                    Id: string.Create(CultureInfo.InvariantCulture, $"event-{index + 1:D4}"),
                    Category: category,
                    CategoryLabel: GetCategoryLabel(category),
                    Tone: GetTone(evt, category),
                    TypeLabel: typeLabel,
                    ActorLabel: actorLabel,
                    MetricLabel: GetMetricLabel(evt),
                    DescriptionHtml: FormatDescriptionHtml(description),
                    DescriptionText: descriptionText,
                    OccurredAtLabel: RunReportFormatting.FormatUtc(evt.OccurredAt),
                    ScenarioTime: RunReportFormatting.FormatScenarioTime(evt.ScenarioFrame),
                    OccurredAtUnixMs: evt.OccurredAt.ToUnixTimeMilliseconds(),
                    Sequence: index,
                    SearchText: searchText,
                    Details: details
                )
            );
        }

        return entries;
    }

    private static Dictionary<string, int> BuildCategoryCounts(IEnumerable<HtmlEventEntry> events)
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        foreach (HtmlEventEntry entry in events)
            counts[entry.Category] = counts.GetValueOrDefault(entry.Category) + 1;

        return counts;
    }

    private static List<HtmlDetailItem> BuildDetails(RunEvent evt, Scenario scenario)
    {
        List<HtmlDetailItem> details =
        [
            new("Occurred At", RunReportFormatting.FormatUtc(evt.OccurredAt)),
            new("Scenario Time", RunReportFormatting.FormatScenarioTime(evt.ScenarioFrame)),
        ];

        switch (evt)
        {
            case DoorDamagedEvent doorDamaged:
                AddIdDetail(details, "Door Id", doorDamaged.DoorId);
                AddDetail(details, "Slot Id", doorDamaged.SlotId);
                AddDetail(details, "Old HP", doorDamaged.OldHp);
                AddDetail(details, "New HP", doorDamaged.NewHp);
                AddDetail(details, "Damage", doorDamaged.Damage);
                break;
            case DoorFlagChangedEvent doorFlagChanged:
                AddIdDetail(details, "Door Id", doorFlagChanged.DoorId);
                AddDetail(details, "Slot Id", doorFlagChanged.SlotId);
                AddDetail(details, "Old Flag", FormatHex(doorFlagChanged.OldFlag));
                AddDetail(details, "New Flag", FormatHex(doorFlagChanged.NewFlag));
                break;
            case DoorStateChangedEvent doorStateChanged:
                AddIdDetail(details, "Door Id", doorStateChanged.DoorId);
                AddDetail(details, "Slot Id", doorStateChanged.SlotId);
                AddDetail(details, "Old Status", doorStateChanged.OldStatus);
                AddDetail(details, "New Status", doorStateChanged.NewStatus);
                break;
            case EnemyDamagedEvent enemyDamaged:
                AddIdDetail(details, "Enemy Id", enemyDamaged.EnemyId);
                AddDetail(details, "Enemy Name", enemyDamaged.EnemyName);
                AddDetail(details, "Slot Id", enemyDamaged.SlotId);
                AddRoomDetail(details, "Room", scenario, enemyDamaged.RoomId);
                AddDetail(details, "Old HP", enemyDamaged.OldHp);
                AddDetail(details, "New HP", enemyDamaged.NewHp);
                AddDetail(details, "Max HP", enemyDamaged.MaxHp);
                AddDetail(details, "Damage", enemyDamaged.Damage);
                AddContributorDetail(details, enemyDamaged.ContributingPlayers);
                break;
            case EnemyDespawnedEvent enemyDespawned:
                AddIdDetail(details, "Enemy Id", enemyDespawned.EnemyId);
                AddDetail(details, "Enemy Name", enemyDespawned.EnemyName);
                AddDetail(details, "Slot Id", enemyDespawned.SlotId);
                AddRoomDetail(details, "Room", scenario, enemyDespawned.RoomId);
                AddDetail(details, "Remaining HP", enemyDespawned.RemainingHp);
                AddDetail(details, "Max HP", enemyDespawned.MaxHp);
                break;
            case EnemyKilledEvent enemyKilled:
                AddIdDetail(details, "Enemy Id", enemyKilled.EnemyId);
                AddDetail(details, "Enemy Name", enemyKilled.EnemyName);
                AddDetail(details, "Slot Id", enemyKilled.SlotId);
                AddRoomDetail(details, "Room", scenario, enemyKilled.RoomId);
                AddContributorDetail(details, enemyKilled.ContributingPlayers);
                break;
            case EnemyRoomChangedEvent enemyRoomChanged:
                AddIdDetail(details, "Enemy Id", enemyRoomChanged.EnemyId);
                AddDetail(details, "Enemy Name", enemyRoomChanged.EnemyName);
                AddDetail(details, "Slot Id", enemyRoomChanged.SlotId);
                AddRoomDetail(details, "Old Room", scenario, enemyRoomChanged.OldRoomId);
                AddRoomDetail(details, "New Room", scenario, enemyRoomChanged.NewRoomId);
                break;
            case EnemySpawnedEvent enemySpawned:
                AddIdDetail(details, "Enemy Id", enemySpawned.EnemyId);
                AddDetail(details, "Enemy Name", enemySpawned.EnemyName);
                AddDetail(details, "Slot Id", enemySpawned.SlotId);
                AddRoomDetail(details, "Room", scenario, enemySpawned.RoomId);
                AddDetail(details, "Max HP", enemySpawned.MaxHp);
                break;
            case EnemyStatusChangedEvent enemyStatusChanged:
                AddIdDetail(details, "Enemy Id", enemyStatusChanged.EnemyId);
                AddDetail(details, "Enemy Name", enemyStatusChanged.EnemyName);
                AddDetail(details, "Slot Id", enemyStatusChanged.SlotId);
                AddRoomDetail(details, "Room", scenario, enemyStatusChanged.RoomId);
                AddDetail(details, "Old Status", FormatByteHex(enemyStatusChanged.OldStatus));
                AddDetail(details, "New Status", FormatByteHex(enemyStatusChanged.NewStatus));
                AddDetail(details, "Activated", enemyStatusChanged.IsActivation ? "Yes" : "No");
                AddContributorDetail(details, enemyStatusChanged.ContributingPlayers);
                break;
            case ItemDroppedEvent itemDropped:
                AddDetail(details, "Item", itemDropped.TypeName);
                AddDetail(details, "Slot Index", itemDropped.SlotIndex);
                AddRoomDetail(details, "Room", scenario, itemDropped.RoomId);
                AddDetail(details, "Previous Holder", itemDropped.PreviousHolder);
                break;
            case ItemPickedUpEvent itemPickedUp:
                AddDetail(details, "Item", itemPickedUp.TypeName);
                AddDetail(details, "Slot Index", itemPickedUp.SlotIndex);
                AddRoomDetail(details, "Room", scenario, itemPickedUp.RoomId);
                AddDetail(details, "Picked Up By", itemPickedUp.PickedUpByName);
                break;
            case ItemQuantityChangedEvent itemQuantityChanged:
                AddDetail(details, "Item", itemQuantityChanged.TypeName);
                AddDetail(details, "Slot Index", itemQuantityChanged.SlotIndex);
                AddRoomDetail(details, "Room", scenario, itemQuantityChanged.RoomId);
                AddDetail(details, "Old Quantity", itemQuantityChanged.OldQuantity);
                AddDetail(details, "New Quantity", itemQuantityChanged.NewQuantity);
                break;
            case PlayerConditionChangedEvent playerConditionChanged:
                AddIdDetail(details, "Player Id", playerConditionChanged.PlayerId);
                AddDetail(details, "Player", playerConditionChanged.PlayerName);
                AddDetail(details, "Old Condition", playerConditionChanged.OldCondition);
                AddDetail(details, "New Condition", playerConditionChanged.NewCondition);
                break;
            case PlayerEffectChangedEvent playerEffectChanged:
                AddIdDetail(details, "Player Id", playerEffectChanged.PlayerId);
                AddDetail(details, "Player", playerEffectChanged.PlayerName);
                AddDetail(details, "Effect", playerEffectChanged.EffectName);
                AddDetail(details, "Action", playerEffectChanged.IsApplied ? "Applied" : "Expired");
                break;
            case PlayerHealthChangedEvent playerHealthChanged:
                AddIdDetail(details, "Player Id", playerHealthChanged.PlayerId);
                AddDetail(details, "Player", playerHealthChanged.PlayerName);
                AddDetail(details, "Old Health", playerHealthChanged.OldHealth);
                AddDetail(details, "New Health", playerHealthChanged.NewHealth);
                AddDetail(details, "Max Health", playerHealthChanged.MaxHealth);
                AddDetail(details, "Delta", FormatSignedValue(playerHealthChanged.Delta, " HP"));
                break;
            case PlayerInventoryChangedEvent playerInventoryChanged:
                AddIdDetail(details, "Player Id", playerInventoryChanged.PlayerId);
                AddDetail(details, "Player", playerInventoryChanged.PlayerName);
                AddDetail(details, "Inventory", playerInventoryChanged.Kind.ToString());
                AddDetail(details, "Slot Index", playerInventoryChanged.SlotIndex);
                AddDetail(details, "Old Item", playerInventoryChanged.OldItemName);
                AddDetail(
                    details,
                    "Old Slot Value",
                    $"0x{playerInventoryChanged.OldItemId:X2} | {playerInventoryChanged.OldItemId}"
                );
                AddDetail(details, "New Item", playerInventoryChanged.NewItemName);
                AddDetail(
                    details,
                    "New Slot Value",
                    $"0x{playerInventoryChanged.NewItemId:X2} | {playerInventoryChanged.NewItemId}"
                );
                break;
            case PlayerJoinedEvent playerJoined:
                AddIdDetail(details, "Player Id", playerJoined.PlayerId);
                AddDetail(details, "Player", playerJoined.PlayerName);
                AddDetail(details, "Initial Health", playerJoined.InitialHealth);
                AddDetail(details, "Max Health", playerJoined.MaxHealth);
                AddDetail(details, "Initial Virus", FormatPercentage(playerJoined.InitialVirusPercentage));
                break;
            case PlayerLeftEvent playerLeft:
                AddIdDetail(details, "Player Id", playerLeft.PlayerId);
                AddDetail(details, "Player", playerLeft.PlayerName);
                AddDetail(details, "Final Health", playerLeft.FinalHealth);
                AddDetail(details, "Final Virus", FormatPercentage(playerLeft.FinalVirusPercentage));
                break;
            case PlayerRoomChangedEvent playerRoomChanged:
                AddIdDetail(details, "Player Id", playerRoomChanged.PlayerId);
                AddDetail(details, "Player", playerRoomChanged.PlayerName);
                AddRoomDetail(details, "Old Room", scenario, playerRoomChanged.OldRoomId);
                AddRoomDetail(details, "New Room", scenario, playerRoomChanged.NewRoomId);
                break;
            case PlayerStatusChangedEvent playerStatusChanged:
                AddIdDetail(details, "Player Id", playerStatusChanged.PlayerId);
                AddDetail(details, "Player", playerStatusChanged.PlayerName);
                AddDetail(details, "Old Status", playerStatusChanged.OldStatus);
                AddDetail(details, "New Status", playerStatusChanged.NewStatus);
                break;
            case PlayerVirusChangedEvent playerVirusChanged:
                AddIdDetail(details, "Player Id", playerVirusChanged.PlayerId);
                AddDetail(details, "Player", playerVirusChanged.PlayerName);
                AddDetail(details, "Old Virus", FormatPercentage(playerVirusChanged.OldVirusPercentage));
                AddDetail(details, "New Virus", FormatPercentage(playerVirusChanged.NewVirusPercentage));
                AddDetail(details, "Delta", FormatSignedPercentage(playerVirusChanged.Delta));
                break;
            case ScenarioStatusChangedEvent scenarioStatusChanged:
                AddDetail(details, "Old Status", scenarioStatusChanged.OldStatus.ToString());
                AddDetail(details, "New Status", scenarioStatusChanged.NewStatus.ToString());
                break;
        }

        return details;
    }

    private static int CountDistinctPlayers(IEnumerable<RunEvent> events)
    {
        HashSet<string> playerNames = new(StringComparer.Ordinal);

        foreach (RunEvent evt in events)
        {
            CollectPlayerNames(playerNames, evt);
        }

        return playerNames.Count;
    }

    private static void CollectPlayerNames(HashSet<string> playerNames, RunEvent evt)
    {
        switch (evt)
        {
            case PlayerConditionChangedEvent playerConditionChanged:
                AddName(playerNames, playerConditionChanged.PlayerName);
                break;
            case PlayerEffectChangedEvent playerEffectChanged:
                AddName(playerNames, playerEffectChanged.PlayerName);
                break;
            case PlayerHealthChangedEvent playerHealthChanged:
                AddName(playerNames, playerHealthChanged.PlayerName);
                break;
            case PlayerInventoryChangedEvent playerInventoryChanged:
                AddName(playerNames, playerInventoryChanged.PlayerName);
                break;
            case PlayerJoinedEvent playerJoined:
                AddName(playerNames, playerJoined.PlayerName);
                break;
            case PlayerLeftEvent playerLeft:
                AddName(playerNames, playerLeft.PlayerName);
                break;
            case PlayerRoomChangedEvent playerRoomChanged:
                AddName(playerNames, playerRoomChanged.PlayerName);
                break;
            case PlayerStatusChangedEvent playerStatusChanged:
                AddName(playerNames, playerStatusChanged.PlayerName);
                break;
            case PlayerVirusChangedEvent playerVirusChanged:
                AddName(playerNames, playerVirusChanged.PlayerName);
                break;
            case ItemPickedUpEvent itemPickedUp:
                AddName(playerNames, itemPickedUp.PickedUpByName);
                break;
            case ItemDroppedEvent itemDropped:
                AddName(playerNames, itemDropped.PreviousHolder);
                break;
            case EnemyDamagedEvent enemyDamaged:
                AddContributorNames(playerNames, enemyDamaged.ContributingPlayers);
                break;
            case EnemyKilledEvent enemyKilled:
                AddContributorNames(playerNames, enemyKilled.ContributingPlayers);
                break;
            case EnemyStatusChangedEvent enemyStatusChanged:
                AddContributorNames(playerNames, enemyStatusChanged.ContributingPlayers);
                break;
        }
    }

    private static void AddContributorNames(
        HashSet<string> playerNames,
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> contributors
    )
    {
        foreach ((_, string playerName, _) in contributors)
            AddName(playerNames, playerName);
    }

    private static void AddName(HashSet<string> playerNames, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            playerNames.Add(value);
    }

    private static string FormatContributors(
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> contributors
    )
    {
        StringBuilder builder = new();

        for (int index = 0; index < contributors.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            (Ulid _, string playerName, float power) = contributors[index];
            builder.Append(CultureInfo.InvariantCulture, $"{playerName} (Power {power:F2})");
        }

        return builder.ToString();
    }

    private static string BuildSearchText(
        string actorLabel,
        string typeLabel,
        string descriptionText,
        IReadOnlyList<HtmlDetailItem> details
    )
    {
        StringBuilder builder = new();
        builder.Append(actorLabel).Append(' ').Append(typeLabel).Append(' ').Append(descriptionText);
        foreach (HtmlDetailItem detail in details)
            builder.Append(' ').Append(detail.Label).Append(' ').Append(detail.Value);

        return builder.ToString();
    }

    private static string GetScenarioTitle(RunReport report)
    {
        if (!string.IsNullOrWhiteSpace(report.ScenarioName))
            return report.ScenarioName;

        if (!string.IsNullOrWhiteSpace(report.ScenarioId))
            return report.ScenarioId;

        return "Unknown Scenario";
    }

    private static string GetCategory(RunEvent evt) =>
        evt switch
        {
            PlayerConditionChangedEvent => "player",
            PlayerEffectChangedEvent => "player",
            PlayerHealthChangedEvent => "player",
            PlayerInventoryChangedEvent => "player",
            PlayerJoinedEvent => "player",
            PlayerLeftEvent => "player",
            PlayerRoomChangedEvent => "player",
            PlayerStatusChangedEvent => "player",
            PlayerVirusChangedEvent => "player",
            EnemyDamagedEvent => "enemy",
            EnemyDespawnedEvent => "enemy",
            EnemyKilledEvent => "enemy",
            EnemyRoomChangedEvent => "enemy",
            EnemySpawnedEvent => "enemy",
            EnemyStatusChangedEvent => "enemy",
            DoorDamagedEvent or DoorFlagChangedEvent or DoorStateChangedEvent => "door",
            ItemDroppedEvent or ItemPickedUpEvent or ItemQuantityChangedEvent => "item",
            ScenarioStatusChangedEvent => "scenario",
            _ => "other",
        };

    private static string GetTypeLabel(RunEvent evt) =>
        evt switch
        {
            DoorDamagedEvent => "Door Damaged",
            DoorFlagChangedEvent => "Door Flag Changed",
            DoorStateChangedEvent => "Door State Changed",
            EnemyDamagedEvent => "Enemy Damaged",
            EnemyDespawnedEvent => "Enemy Despawned",
            EnemyKilledEvent => "Enemy Killed",
            EnemyRoomChangedEvent => "Enemy Room Changed",
            EnemySpawnedEvent => "Enemy Spawned",
            EnemyStatusChangedEvent => "Enemy Status Changed",
            ItemDroppedEvent => "Item Dropped",
            ItemPickedUpEvent => "Item Picked Up",
            ItemQuantityChangedEvent => "Item Quantity Changed",
            PlayerConditionChangedEvent => "Player Condition Changed",
            PlayerEffectChangedEvent => "Player Effect Changed",
            PlayerHealthChangedEvent => "Player Health Changed",
            PlayerInventoryChangedEvent => "Player Inventory Changed",
            PlayerJoinedEvent => "Player Joined",
            PlayerLeftEvent => "Player Left",
            PlayerRoomChangedEvent => "Player Room Changed",
            PlayerStatusChangedEvent => "Player Status Changed",
            PlayerVirusChangedEvent => "Player Virus Changed",
            ScenarioStatusChangedEvent => "Scenario Status Changed",
            _ => "Run Event",
        };

    private static string GetCategoryLabel(string category) =>
        category switch
        {
            "player" => "Players",
            "enemy" => "Enemies",
            "door" => "Doors",
            "item" => "Items",
            "scenario" => "Scenario",
            _ => "Other",
        };

    private static string GetActorLabel(RunEvent evt) =>
        evt switch
        {
            PlayerConditionChangedEvent playerConditionChanged => playerConditionChanged.PlayerName,
            PlayerEffectChangedEvent playerEffectChanged => playerEffectChanged.PlayerName,
            PlayerHealthChangedEvent playerHealthChanged => playerHealthChanged.PlayerName,
            PlayerInventoryChangedEvent playerInventoryChanged => playerInventoryChanged.PlayerName,
            PlayerJoinedEvent playerJoined => playerJoined.PlayerName,
            PlayerLeftEvent playerLeft => playerLeft.PlayerName,
            PlayerRoomChangedEvent playerRoomChanged => playerRoomChanged.PlayerName,
            PlayerStatusChangedEvent playerStatusChanged => playerStatusChanged.PlayerName,
            PlayerVirusChangedEvent playerVirusChanged => playerVirusChanged.PlayerName,
            EnemyDamagedEvent enemyDamaged => enemyDamaged.EnemyName,
            EnemyDespawnedEvent enemyDespawned => enemyDespawned.EnemyName,
            EnemyKilledEvent enemyKilled => enemyKilled.EnemyName,
            EnemyRoomChangedEvent enemyRoomChanged => enemyRoomChanged.EnemyName,
            EnemySpawnedEvent enemySpawned => enemySpawned.EnemyName,
            EnemyStatusChangedEvent enemyStatusChanged => enemyStatusChanged.EnemyName,
            ItemDroppedEvent itemDropped => itemDropped.TypeName,
            ItemPickedUpEvent itemPickedUp => itemPickedUp.TypeName,
            ItemQuantityChangedEvent itemQuantityChanged => itemQuantityChanged.TypeName,
            DoorDamagedEvent doorDamaged => FormatDoorLabel(doorDamaged.SlotId),
            DoorFlagChangedEvent doorFlagChanged => FormatDoorLabel(doorFlagChanged.SlotId),
            DoorStateChangedEvent doorStateChanged => FormatDoorLabel(doorStateChanged.SlotId),
            ScenarioStatusChangedEvent => "Scenario",
            _ => "Run Event",
        };

    private static string? GetMetricLabel(RunEvent evt) =>
        evt switch
        {
            EnemyDamagedEvent enemyDamaged => string.Create(CultureInfo.InvariantCulture, $"-{enemyDamaged.Damage} HP"),
            EnemyKilledEvent => "Kill",
            EnemyDespawnedEvent => "Despawn",
            PlayerHealthChangedEvent playerHealth when playerHealth.IsDamage => string.Create(
                CultureInfo.InvariantCulture,
                $"-{playerHealth.OldHealth - playerHealth.NewHealth} HP"
            ),
            PlayerHealthChangedEvent playerHealth when playerHealth.IsHeal => string.Create(
                CultureInfo.InvariantCulture,
                $"+{playerHealth.NewHealth - playerHealth.OldHealth} HP"
            ),
            PlayerVirusChangedEvent virus => string.Create(
                CultureInfo.InvariantCulture,
                $"{virus.Delta:+0.000;-0.000;0.000}%"
            ),
            DoorDamagedEvent doorDamaged => string.Create(CultureInfo.InvariantCulture, $"-{doorDamaged.Damage} HP"),
            ItemQuantityChangedEvent quantity => string.Create(
                CultureInfo.InvariantCulture,
                $"{quantity.OldQuantity} -> {quantity.NewQuantity}"
            ),
            ScenarioStatusChangedEvent status => string.Create(
                CultureInfo.InvariantCulture,
                $"{status.OldStatus} -> {status.NewStatus}"
            ),
            _ => null,
        };

    private static string GetTone(RunEvent evt, string category) =>
        evt switch
        {
            EnemyKilledEvent or ItemPickedUpEvent or PlayerHealthChangedEvent { IsHeal: true } => "good",
            PlayerHealthChangedEvent { IsDamage: true } or PlayerVirusChangedEvent { Delta: > 0 } or DoorDamagedEvent =>
                "warn",
            EnemyDespawnedEvent => "muted",
            _ => category,
        };

    private static string StripMarkdown(string value) => value.Replace("**", string.Empty, StringComparison.Ordinal);

    private static string FormatDescriptionHtml(string description)
    {
        StringBuilder builder = new(description.Length + 16);
        bool isStrong = false;
        int currentIndex = 0;

        while (currentIndex < description.Length)
        {
            int markerIndex = description.IndexOf("**", currentIndex, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                AppendEncoded(builder, description[currentIndex..]);
                break;
            }

            AppendEncoded(builder, description[currentIndex..markerIndex]);
            builder.Append(isStrong ? "</strong>" : "<strong>");
            isStrong = !isStrong;
            currentIndex = markerIndex + 2;
        }

        if (isStrong)
            builder.Append("</strong>");

        return builder.ToString();
    }

    private static void AppendEncoded(StringBuilder builder, string value) =>
        builder.Append(WebUtility.HtmlEncode(value));

    private static void AddIdDetail(List<HtmlDetailItem> details, string label, Ulid value) =>
        AddDetail(details, label, value.ToString(null, CultureInfo.InvariantCulture));

    private static void AddRoomDetail(List<HtmlDetailItem> details, string label, Scenario scenario, int roomId) =>
        AddDetail(
            details,
            label,
            string.Create(CultureInfo.InvariantCulture, $"{scenario.GetRoomName(roomId)} ({roomId})")
        );

    private static void AddContributorDetail(
        List<HtmlDetailItem> details,
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> contributors
    ) => AddDetail(details, "Contributors", contributors.Count == 0 ? "None" : FormatContributors(contributors));

    private static void AddDetail(List<HtmlDetailItem> details, string label, object? value)
    {
        string formatted = value switch
        {
            null => string.Empty,
            string text => string.IsNullOrWhiteSpace(text) ? string.Empty : text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

        if (!string.IsNullOrWhiteSpace(formatted))
            details.Add(new HtmlDetailItem(label, formatted));
    }

    private static string FormatDoorLabel(int slotId) => string.Create(CultureInfo.InvariantCulture, $"Door #{slotId}");

    private static string FormatHex(ushort value) => string.Create(CultureInfo.InvariantCulture, $"0x{value:X4}");

    private static string FormatByteHex(byte value) => string.Create(CultureInfo.InvariantCulture, $"0x{value:X2}");

    private static string FormatPercentage(double value) => string.Create(CultureInfo.InvariantCulture, $"{value:F3}%");

    private static string FormatSignedPercentage(double value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.000;-0.000;0.000}%");

    private static string FormatSignedValue(short value, string suffix) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+#;-#;0}{suffix}");

    private static readonly string[] CategoryOrder = ["player", "enemy", "door", "item", "scenario", "other"];

    internal sealed record HtmlPayload(
        string ScenarioTitle,
        string SessionId,
        string DurationText,
        long DurationMs,
        int TotalEvents,
        List<HtmlEventEntry> Events
    );

    internal sealed record HtmlEventEntry(
        string Id,
        string Category,
        string CategoryLabel,
        string Tone,
        string TypeLabel,
        string ActorLabel,
        string? MetricLabel,
        string DescriptionHtml,
        string DescriptionText,
        string OccurredAtLabel,
        string ScenarioTime,
        long OccurredAtUnixMs,
        int Sequence,
        string SearchText,
        List<HtmlDetailItem> Details
    );

    internal sealed record HtmlDetailItem(string Label, string Value);

    private const string Styles = """
:root {
    --bg: #0c1117;
    --surface-0: rgba(13, 18, 27, 0.88);
    --surface-1: rgba(18, 24, 36, 0.92);
    --surface-2: rgba(32, 40, 56, 0.92);
    --surface-3: rgba(52, 63, 84, 0.92);
    --border: rgba(255, 255, 255, 0.08);
    --border-strong: rgba(255, 255, 255, 0.16);
    --text: #edf2f7;
    --text-muted: #a7b1c2;
    --text-soft: #7e889a;
    --accent: #4fd1c5;
    --player: #34d399;
    --enemy: #fb7185;
    --door: #f59e0b;
    --item: #60a5fa;
    --scenario: #c084fc;
    --other: #94a3b8;
    --good: #34d399;
    --warn: #f59e0b;
    --muted: #94a3b8;
    --shadow: 0 24px 80px rgba(0, 0, 0, 0.35);
    --radius-lg: 28px;
    --radius-md: 18px;
    --radius-sm: 12px;
    --font-ui: "Segoe UI", "Aptos", sans-serif;
    --font-mono: "Cascadia Code", "Consolas", monospace;
}

:root[data-theme="light"] {
    --bg: #edf3f7;
    --surface-0: rgba(255, 255, 255, 0.84);
    --surface-1: rgba(255, 255, 255, 0.92);
    --surface-2: rgba(234, 241, 247, 0.96);
    --surface-3: rgba(216, 226, 236, 0.98);
    --border: rgba(15, 23, 42, 0.08);
    --border-strong: rgba(15, 23, 42, 0.16);
    --text: #152033;
    --text-muted: #4d5b72;
    --text-soft: #6a778d;
    --shadow: 0 24px 60px rgba(52, 72, 100, 0.16);
}

* {
    box-sizing: border-box;
}

html {
    color-scheme: dark;
    scroll-behavior: smooth;
}

html[data-theme="light"] {
    color-scheme: light;
}

body {
    margin: 0;
    font-family: var(--font-ui);
    background:
        radial-gradient(circle at top left, rgba(79, 209, 197, 0.18), transparent 28%),
        radial-gradient(circle at top right, rgba(251, 113, 133, 0.16), transparent 24%),
        linear-gradient(180deg, #0c1117 0%, #111827 48%, #0b0f16 100%);
    color: var(--text);
    min-height: 100vh;
}

html[data-theme="light"] body {
    background:
        radial-gradient(circle at top left, rgba(79, 209, 197, 0.14), transparent 26%),
        radial-gradient(circle at top right, rgba(251, 113, 133, 0.12), transparent 20%),
        linear-gradient(180deg, #eff5f8 0%, #edf3f7 50%, #e8eef5 100%);
}

.page-shell {
    width: min(1400px, calc(100% - 40px));
    margin: 0 auto;
    padding: 32px 0 72px;
}

.page-header,
.hero-panel,
.insight-panel,
.toolbar,
.empty-state,
.group-card,
.event-card,
.sticky-bar {
    backdrop-filter: blur(20px);
    background: var(--surface-0);
    border: 1px solid var(--border);
    box-shadow: var(--shadow);
}

.page-header {
    display: flex;
    gap: 24px;
    justify-content: space-between;
    align-items: flex-start;
    border-radius: var(--radius-lg);
    padding: 28px 30px;
    margin-bottom: 20px;
}

.eyebrow,
.panel-kicker,
.toolbar-label,
.search-label,
.meta-key,
.stat-label {
    text-transform: uppercase;
    letter-spacing: 0.12em;
    font-size: 0.72rem;
    color: var(--text-soft);
}

.page-header h1,
.empty-state h2 {
    margin: 8px 0 10px;
    font-size: clamp(2rem, 4vw, 3.6rem);
    line-height: 0.95;
    letter-spacing: -0.04em;
}

.subtitle,
.panel-note,
.empty-state p,
.filter-summary,
.detail-list,
.event-meta,
.event-body {
    color: var(--text-muted);
}

.meta-row {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-top: 18px;
}

.meta-chip,
.sticky-pill,
.filter-pill,
.toolbar-btn,
.toggle-btn,
.event-chip,
.detail-chip,
.copy-btn,
.theme-toggle,
.sticky-search {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    border: 1px solid var(--border);
    border-radius: 999px;
    background: var(--surface-1);
    color: var(--text);
}

.meta-chip {
    padding: 8px 12px;
}

.meta-value,
.ranking-value,
.event-time,
.detail-value,
.stat-value {
    font-variant-numeric: tabular-nums;
}

.theme-toggle,
.sticky-search,
.toolbar-btn,
.toggle-btn,
.filter-pill,
.copy-btn,
.group-toggle,
.event-open {
    cursor: pointer;
    transition: border-color 140ms ease, transform 140ms ease, background 140ms ease, color 140ms ease;
}

.theme-toggle,
.sticky-search {
    padding: 12px 16px;
    font-weight: 600;
}

.theme-toggle:hover,
.sticky-search:hover,
.toolbar-btn:hover,
.toggle-btn:hover,
.filter-pill:hover,
.copy-btn:hover,
.group-toggle:hover,
.event-open:hover {
    border-color: var(--border-strong);
    transform: translateY(-1px);
}

.hero-grid,
.insight-grid {
    display: grid;
    gap: 16px;
}

.hero-grid {
    grid-template-columns: minmax(300px, 360px) minmax(0, 1fr);
    margin-bottom: 16px;
}

.hero-panel,
.insight-panel {
    border-radius: var(--radius-lg);
    padding: 24px;
}

.ring-panel {
    display: flex;
    flex-direction: column;
    gap: 18px;
}

.ring-shell {
    position: relative;
    width: 100%;
    max-width: 220px;
    margin: 0 auto;
}

.hero-ring {
    width: 100%;
    height: auto;
}

.ring-segment.player {
    stroke: var(--player);
}

.ring-segment.enemy {
    stroke: var(--enemy);
}

.ring-segment.door {
    stroke: var(--door);
}

.ring-segment.item {
    stroke: var(--item);
}

.ring-segment.scenario {
    stroke: var(--scenario);
}

.ring-segment.other {
    stroke: var(--other);
}

.ring-center {
    position: absolute;
    inset: 0;
    display: grid;
    place-items: center;
    text-align: center;
}

.ring-value {
    display: block;
    font-size: 2.6rem;
    font-weight: 800;
    letter-spacing: -0.05em;
}

.ring-label {
    display: block;
    font-size: 0.82rem;
    color: var(--text-soft);
}

.mix-legend,
.ranking-list,
.group-list {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.legend-row,
.ranking-row {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    align-items: center;
}

.legend-dot {
    width: 10px;
    height: 10px;
    border-radius: 999px;
    display: inline-block;
    margin-right: 10px;
}

.legend-dot.player { background: var(--player); }
.legend-dot.enemy { background: var(--enemy); }
.legend-dot.door { background: var(--door); }
.legend-dot.item { background: var(--item); }
.legend-dot.scenario { background: var(--scenario); }
.legend-dot.other { background: var(--other); }

.legend-name,
.ranking-name {
    flex: 1;
    min-width: 0;
}

.stats-panel {
    display: flex;
    flex-direction: column;
    gap: 18px;
}

.stats-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 12px;
}

.stat-card {
    padding: 16px;
    border-radius: var(--radius-md);
    background: var(--surface-1);
    border: 1px solid var(--border);
}

.stat-card.player .stat-value { color: var(--player); }
.stat-card.enemy .stat-value { color: var(--enemy); }
.stat-card.warn .stat-value { color: var(--door); }
.stat-card.accent .stat-value { color: var(--accent); }

.stat-value {
    display: block;
    margin-top: 8px;
    font-size: 1.5rem;
    font-weight: 750;
    letter-spacing: -0.04em;
}

.histogram-block {
    padding-top: 4px;
}

.histogram {
    display: flex;
    align-items: end;
    gap: 4px;
    height: 42px;
    margin-top: 10px;
}

.histogram-bar {
    flex: 1;
    min-width: 8px;
    border-radius: 999px 999px 4px 4px;
    background: linear-gradient(180deg, var(--accent), rgba(79, 209, 197, 0.2));
}

.insight-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
    margin-bottom: 16px;
}

.panel-note,
.panel-empty {
    margin: 6px 0 14px;
}

.toolbar {
    display: grid;
    grid-template-columns: minmax(220px, 1.3fr) minmax(0, 1.5fr) repeat(2, minmax(180px, 0.8fr)) auto;
    gap: 12px;
    padding: 16px;
    border-radius: var(--radius-lg);
    margin-bottom: 16px;
    align-items: end;
}

.search-box,
.toolbar-section,
.toolbar-actions {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.search-box input {
    width: 100%;
    padding: 14px 16px;
    border-radius: var(--radius-sm);
    border: 1px solid var(--border);
    background: var(--surface-1);
    color: var(--text);
}

.search-box input:focus {
    outline: none;
    border-color: var(--accent);
    box-shadow: 0 0 0 4px rgba(79, 209, 197, 0.12);
}

.filter-row,
.toggle-row {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}

.filter-pill,
.toggle-btn,
.toolbar-btn,
.copy-btn {
    padding: 10px 14px;
    font-weight: 600;
}

.filter-pill.active,
.toggle-btn.active {
    background: linear-gradient(135deg, rgba(79, 209, 197, 0.22), rgba(96, 165, 250, 0.18));
    border-color: rgba(79, 209, 197, 0.38);
}

.filter-count {
    color: var(--text-soft);
}

.toolbar-actions {
    align-items: flex-end;
    justify-content: flex-end;
}

.filter-summary {
    min-height: 1.2em;
    text-align: right;
}

.group-list {
    gap: 12px;
}

.group-card,
.event-card,
.empty-state,
.sticky-bar {
    border-radius: var(--radius-md);
}

.group-card {
    overflow: hidden;
}

.group-toggle,
.event-open {
    width: 100%;
    text-align: left;
    border: none;
    background: transparent;
    color: inherit;
}

.group-toggle {
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 18px;
    padding: 18px 20px;
}

.group-title {
    display: flex;
    align-items: center;
    gap: 12px;
    font-weight: 700;
    font-size: 1rem;
}

.group-meta {
    display: block;
    margin-top: 6px;
    color: var(--text-soft);
    font-size: 0.86rem;
}

.group-count {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 42px;
    padding: 8px 12px;
    border-radius: 999px;
    background: var(--surface-2);
    border: 1px solid var(--border);
    font-weight: 700;
    font-variant-numeric: tabular-nums;
}

.group-events {
    display: grid;
    gap: 12px;
    padding: 0 16px 16px;
}

.event-card {
    overflow: hidden;
    background: var(--surface-1);
}

.event-toggle {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 14px;
    align-items: start;
    padding: 16px;
}

.event-open {
    display: grid;
    grid-template-columns: auto 1fr auto;
    gap: 14px;
    align-items: start;
    padding: 0;
}

.event-toggle.highlight {
    box-shadow: inset 0 0 0 2px rgba(79, 209, 197, 0.4);
    border-radius: var(--radius-sm);
}

.event-badges,
.event-actions,
.event-topline,
.event-details {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}

.event-chip,
.detail-chip {
    padding: 6px 10px;
    font-size: 0.78rem;
}

.event-chip.player { background: rgba(52, 211, 153, 0.14); color: var(--player); }
.event-chip.enemy { background: rgba(251, 113, 133, 0.14); color: var(--enemy); }
.event-chip.door { background: rgba(245, 158, 11, 0.16); color: var(--door); }
.event-chip.item { background: rgba(96, 165, 250, 0.16); color: var(--item); }
.event-chip.scenario { background: rgba(192, 132, 252, 0.16); color: var(--scenario); }
.event-chip.other,
.event-chip.muted { background: rgba(148, 163, 184, 0.16); color: var(--other); }
.event-chip.good { background: rgba(52, 211, 153, 0.14); color: var(--good); }
.event-chip.warn { background: rgba(245, 158, 11, 0.16); color: var(--warn); }

.event-main {
    min-width: 0;
}

.event-main h3 {
    margin: 0;
    font-size: 1rem;
}

.event-body {
    margin: 10px 0 0;
    line-height: 1.55;
}

.event-body strong {
    color: var(--text);
}

.event-time {
    font-family: var(--font-mono);
    white-space: nowrap;
    text-align: right;
    line-height: 1.6;
}

.event-actions {
    justify-content: flex-end;
}

.copy-btn {
    min-width: 74px;
    justify-content: center;
}

.event-detail {
    display: none;
    padding: 0 16px 16px;
}

.event-card.open .event-detail {
    display: block;
}

.detail-list {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
}

.detail-chip {
    background: var(--surface-2);
}

.detail-key {
    color: var(--text-soft);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.67rem;
}

.detail-value {
    color: var(--text);
}

.empty-state {
    padding: 44px 28px;
    text-align: center;
    margin-bottom: 12px;
}

.sticky-bar {
    position: sticky;
    top: 12px;
    z-index: 10;
    width: min(1400px, calc(100% - 40px));
    margin: 0 auto 12px;
    padding: 12px 14px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    opacity: 0;
    transform: translateY(-18px);
    pointer-events: none;
}

.sticky-bar.visible {
    opacity: 1;
    transform: translateY(0);
    pointer-events: auto;
}

.sticky-copy,
.sticky-metrics {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
}

.sticky-name {
    font-weight: 700;
}

mark {
    background: rgba(245, 158, 11, 0.24);
    color: inherit;
    padding: 0 2px;
    border-radius: 4px;
}

@media (max-width: 1120px) {
    .hero-grid,
    .insight-grid,
    .toolbar {
        grid-template-columns: 1fr;
    }

    .toolbar-actions {
        align-items: flex-start;
    }
}

@media (max-width: 820px) {
    .page-shell,
    .sticky-bar {
        width: min(100% - 24px, 1000px);
    }

    .page-header {
        flex-direction: column;
    }

    .stats-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    .event-toggle {
        grid-template-columns: 1fr;
    }

    .event-open {
        grid-template-columns: 1fr;
    }

    .event-time,
    .event-actions {
        text-align: left;
        justify-content: flex-start;
    }
}

@media (max-width: 560px) {
    .stats-grid {
        grid-template-columns: 1fr;
    }

    .page-header,
    .hero-panel,
    .insight-panel,
    .toolbar,
    .empty-state {
        padding: 20px;
    }

    .page-shell {
        padding-top: 20px;
    }

    .sticky-bar {
        top: 8px;
    }
}
""";

    private const string ClientScript = """
(() => {
    const payload = JSON.parse(document.getElementById('report-data')?.textContent ?? '{}');
    const categories = [
        { key: 'all', label: 'All', tone: 'other' },
        { key: 'player', label: 'Players', tone: 'player' },
        { key: 'enemy', label: 'Enemies', tone: 'enemy' },
        { key: 'door', label: 'Doors', tone: 'door' },
        { key: 'item', label: 'Items', tone: 'item' },
        { key: 'scenario', label: 'Scenario', tone: 'scenario' },
        { key: 'other', label: 'Other', tone: 'other' },
    ];

    const state = {
        query: '',
        category: 'all',
        groupBy: 'category',
        sort: 'timeline',
        expandedGroups: new Map(),
        expandedEvents: new Set(),
    };

    const searchInput = document.getElementById('eventSearch');
    const clearSearchButton = document.getElementById('clearSearch');
    const filterHost = document.getElementById('categoryFilters');
    const groupsHost = document.getElementById('eventGroups');
    const filterSummary = document.getElementById('filterSummary');
    const emptyState = document.getElementById('emptyState');
    const histogram = document.getElementById('eventHistogram');
    const stickyBar = document.getElementById('stickyBar');
    const stickySearchButton = document.getElementById('stickySearchBtn');
    const themeToggle = document.getElementById('themeToggle');

    const escapeHtml = (value) => String(value)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');

    const escapeRegExp = (value) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

    const highlight = (value, query) => {
        const text = String(value);
        if (!query) {
            return escapeHtml(text);
        }

        const expression = new RegExp(`(${escapeRegExp(query)})`, 'ig');
        return text
            .split(expression)
            .map((part) => part.toLowerCase() === query.toLowerCase() ? `<mark>${escapeHtml(part)}</mark>` : escapeHtml(part))
            .join('');
    };

    const themeStorageKey = 'ot2-run-report-theme';
    const setTheme = (theme) => {
        document.documentElement.dataset.theme = theme;
        localStorage.setItem(themeStorageKey, theme);
    };

    const initializeTheme = () => {
        const storedTheme = localStorage.getItem(themeStorageKey);
        if (storedTheme === 'light' || storedTheme === 'dark') {
            setTheme(storedTheme);
            return;
        }

        setTheme(window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
    };

    const categoryCounts = (events) => {
        const counts = { all: events.length };
        for (const event of events) {
            counts[event.category] = (counts[event.category] ?? 0) + 1;
        }

        return counts;
    };

    const getFilteredEvents = () => {
        const normalizedQuery = state.query.trim().toLowerCase();
        return payload.events.filter((event) => {
            if (state.category !== 'all' && event.category !== state.category) {
                return false;
            }

            if (!normalizedQuery) {
                return true;
            }

            return event.searchText.toLowerCase().includes(normalizedQuery);
        });
    };

    const sortEvents = (events) => {
        const sorted = [...events];
        switch (state.sort) {
            case 'newest':
                sorted.sort((left, right) => right.sequence - left.sequence);
                break;
            case 'name':
                sorted.sort((left, right) => {
                    const nameComparison = left.actorLabel.localeCompare(right.actorLabel);
                    return nameComparison !== 0 ? nameComparison : left.typeLabel.localeCompare(right.typeLabel);
                });
                break;
            default:
                sorted.sort((left, right) => left.sequence - right.sequence);
                break;
        }

        return sorted;
    };

    const groupEvents = (events) => {
        const groups = new Map();

        for (const event of sortEvents(events)) {
            let groupKey = '';
            let groupLabel = '';

            switch (state.groupBy) {
                case 'type':
                    groupKey = event.typeLabel;
                    groupLabel = event.typeLabel;
                    break;
                case 'actor':
                    groupKey = event.actorLabel;
                    groupLabel = event.actorLabel;
                    break;
                default:
                    groupKey = event.category;
                    groupLabel = event.categoryLabel;
                    break;
            }

            if (!groups.has(groupKey)) {
                groups.set(groupKey, { key: groupKey, label: groupLabel, tone: event.category, events: [] });
            }

            groups.get(groupKey).events.push(event);
        }

        const values = [...groups.values()];
        if (state.sort === 'name') {
            values.sort((left, right) => left.label.localeCompare(right.label));
        } else if (state.sort === 'newest') {
            values.sort((left, right) => right.events[0].sequence - left.events[0].sequence);
        } else {
            values.sort((left, right) => left.events[0].sequence - right.events[0].sequence);
        }

        return values;
    };

    const isGroupOpen = (groupKey, index) => {
        const key = `${state.groupBy}:${groupKey}`;
        return state.expandedGroups.has(key) ? state.expandedGroups.get(key) : index < 3;
    };

    const renderFilters = () => {
        const counts = categoryCounts(payload.events);
        filterHost.innerHTML = categories.map((category) => {
            const active = category.key === state.category;
            const count = counts[category.key] ?? 0;
            return `
                <button class="filter-pill ${active ? 'active' : ''}" type="button" data-category="${category.key}" aria-pressed="${active}">
                    <span class="legend-dot ${category.tone}"></span>
                    <span>${escapeHtml(category.label)}</span>
                    <span class="filter-count">${count}</span>
                </button>`;
        }).join('');
    };

    const renderHistogram = () => {
        const bucketCount = 18;
        const buckets = Array.from({ length: bucketCount }, () => 0);
        const duration = Math.max(1, Number(payload.durationMs));
        const origin = payload.events.length === 0 ? 0 : payload.events[0].occurredAtUnixMs;

        for (const event of payload.events) {
            const ratio = Math.min(0.9999, Math.max(0, event.occurredAtUnixMs - origin) / duration);
            const bucketIndex = Math.min(bucketCount - 1, Math.floor(ratio * bucketCount));
            buckets[bucketIndex] += 1;
        }

        const peak = Math.max(1, ...buckets);
        histogram.innerHTML = buckets.map((value) => {
            const height = Math.max(14, Math.round((value / peak) * 100));
            return `<span class="histogram-bar" style="height:${height}%" title="${value} event(s)"></span>`;
        }).join('');
    };

    const formatGroupMeta = (events) => `${events.length} event(s) from ${events[0].scenarioTime} to ${events[events.length - 1].scenarioTime}`;

    const renderEvent = (event) => {
        const description = state.query ? highlight(event.descriptionText, state.query) : event.descriptionHtml;
        const metricChip = event.metricLabel ? `<span class="event-chip ${event.tone}">${escapeHtml(event.metricLabel)}</span>` : '';
        const detailHtml = event.details.map((detail) => `
            <div class="detail-chip">
                <span class="detail-key">${escapeHtml(detail.label)}</span>
                <span class="detail-value">${highlight(detail.value, state.query)}</span>
            </div>`).join('');
        const isOpen = state.expandedEvents.has(event.id);

        return `
            <article class="event-card ${isOpen ? 'open' : ''}" id="${event.id}">
                <div class="event-toggle${location.hash === `#${event.id}` ? ' highlight' : ''}">
                    <button class="event-open" type="button" data-event-id="${event.id}" aria-expanded="${isOpen}">
                        <div class="event-badges">
                            <span class="event-chip ${event.category}">${escapeHtml(event.categoryLabel)}</span>
                            <span class="event-chip ${event.tone}">${escapeHtml(event.typeLabel)}</span>
                            ${metricChip}
                        </div>
                        <div class="event-main">
                            <div class="event-topline"><h3>${highlight(event.actorLabel, state.query)}</h3></div>
                            <p class="event-body">${description}</p>
                        </div>
                        <div class="event-time">
                            <div>${escapeHtml(event.scenarioTime)}</div>
                            <div>${escapeHtml(event.occurredAtLabel)}</div>
                        </div>
                    </button>
                    <div class="event-actions">
                        <button class="copy-btn" type="button" data-copy-id="${event.id}">Copy link</button>
                    </div>
                </div>
                <div class="event-detail">
                    <div class="detail-list">${detailHtml}</div>
                </div>
            </article>`;
    };

    const renderGroups = () => {
        const filteredEvents = getFilteredEvents();
        const groups = groupEvents(filteredEvents);

        filterSummary.textContent = `${filteredEvents.length} of ${payload.totalEvents} events shown`;
        emptyState.hidden = filteredEvents.length !== 0;
        groupsHost.innerHTML = groups.map((group, index) => {
            const open = isGroupOpen(group.key, index);
            const storageKey = `${state.groupBy}:${group.key}`;
            return `
                <section class="group-card">
                    <button class="group-toggle" type="button" data-group-key="${storageKey}" aria-expanded="${open}">
                        <div>
                            <div class="group-title">
                                <span class="legend-dot ${group.tone}"></span>
                                <span>${escapeHtml(group.label)}</span>
                            </div>
                            <span class="group-meta">${escapeHtml(formatGroupMeta(group.events))}</span>
                        </div>
                        <span class="group-count">${group.events.length}</span>
                    </button>
                    <div class="group-events" ${open ? '' : 'hidden'}>
                        ${group.events.map(renderEvent).join('')}
                    </div>
                </section>`;
        }).join('');
    };

    const setActiveButtons = (selector, activeValue, dataAttribute) => {
        document.querySelectorAll(selector).forEach((button) => {
            const active = button.dataset[dataAttribute] === activeValue;
            button.classList.toggle('active', active);
            button.setAttribute('aria-pressed', String(active));
        });
    };

    const expandGroupsForHash = () => {
        const hash = location.hash.startsWith('#event-') ? location.hash.slice(1) : '';
        if (!hash) {
            return;
        }

        const event = payload.events.find((candidate) => candidate.id === hash);
        if (!event) {
            return;
        }

        state.expandedEvents.add(event.id);

        const groupKey = state.groupBy === 'type'
            ? event.typeLabel
            : state.groupBy === 'actor'
                ? event.actorLabel
                : event.category;

        state.expandedGroups.set(`${state.groupBy}:${groupKey}`, true);
    };

    const copyDeepLink = async (eventId) => {
        const url = new URL(window.location.href);
        url.hash = eventId;
        try {
            await navigator.clipboard.writeText(url.toString());
        } catch {
            window.prompt('Copy event link', url.toString());
        }

        location.hash = eventId;
    };

    const render = () => {
        renderFilters();
        setActiveButtons('[data-group]', state.groupBy, 'group');
        setActiveButtons('[data-sort]', state.sort, 'sort');
        expandGroupsForHash();
        renderGroups();
    };

    filterHost.addEventListener('click', (event) => {
        const button = event.target.closest('[data-category]');
        if (!button) {
            return;
        }

        state.category = button.dataset.category;
        render();
    });

    document.querySelectorAll('[data-group]').forEach((button) => {
        button.addEventListener('click', () => {
            state.groupBy = button.dataset.group;
            render();
        });
    });

    document.querySelectorAll('[data-sort]').forEach((button) => {
        button.addEventListener('click', () => {
            state.sort = button.dataset.sort;
            render();
        });
    });

    searchInput.addEventListener('input', () => {
        state.query = searchInput.value;
        render();
    });

    clearSearchButton.addEventListener('click', () => {
        searchInput.value = '';
        state.query = '';
        render();
        searchInput.focus();
    });

    document.getElementById('expandAll').addEventListener('click', () => {
        for (const group of groupEvents(getFilteredEvents())) {
            state.expandedGroups.set(`${state.groupBy}:${group.key}`, true);
        }
        render();
    });

    document.getElementById('collapseAll').addEventListener('click', () => {
        for (const group of groupEvents(getFilteredEvents())) {
            state.expandedGroups.set(`${state.groupBy}:${group.key}`, false);
        }
        render();
    });

    groupsHost.addEventListener('click', (event) => {
        const copyButton = event.target.closest('[data-copy-id]');
        if (copyButton) {
            event.preventDefault();
            event.stopPropagation();
            copyDeepLink(copyButton.dataset.copyId);
            return;
        }

        const groupButton = event.target.closest('[data-group-key]');
        if (groupButton) {
            const current = state.expandedGroups.get(groupButton.dataset.groupKey) ?? false;
            state.expandedGroups.set(groupButton.dataset.groupKey, !current);
            render();
            return;
        }

        const eventButton = event.target.closest('[data-event-id]');
        if (eventButton) {
            const eventId = eventButton.dataset.eventId;
            if (state.expandedEvents.has(eventId)) {
                state.expandedEvents.delete(eventId);
            } else {
                state.expandedEvents.add(eventId);
            }
            render();
        }
    });

    const updateStickyBar = () => {
        stickyBar.classList.toggle('visible', window.scrollY > 220);
    };

    window.addEventListener('scroll', updateStickyBar, { passive: true });
    window.addEventListener('hashchange', render);
    stickySearchButton.addEventListener('click', () => searchInput.focus());
    themeToggle.addEventListener('click', () => setTheme(document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'));

    initializeTheme();
    renderHistogram();
    render();
    updateStickyBar();
})();
""";
}

using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Reports;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.UnitTests;

public sealed class MarkdownRunReportWriterTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static (
        MarkdownRunReportWriter Writer,
        string OutputDirectory,
        IDisposable Cleanup
    ) CreateWriterWithTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"OT2.Reports.{Guid.NewGuid():N}");
        MarkdownRunReportWriter writer = new(
            NullLogger<MarkdownRunReportWriter>.Instance,
            new RunReportOptions { OutputDirectory = dir }
        );
        IDisposable cleanup = new TempDirectoryCleanup(dir);
        return (writer, dir, cleanup);
    }

    private static RunReport EmptyReport(Ulid? sessionId = null) =>
        new(
            sessionId ?? Ulid.NewUlid(),
            "training-ground",
            "Training Ground",
            Scenario.TrainingGround,
            new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 15, 10, 5, 30, TimeSpan.Zero),
            []
        );

    private static async Task<string> ReadSingleFileAsync(string directory)
    {
        string[] files = Directory.GetFiles(directory, "*.md");
        return await File.ReadAllTextAsync(files[0]);
    }

    // ── output directory & file naming ───────────────────────────────────────

    [Test]
    public async Task WriteAsync_UsesConfiguredOutputDirectory()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            RunReport report = EmptyReport();
            await writer.WriteAsync(report);

            string[] files = Directory.GetFiles(outputDirectory, "*.md");
            await Assert.That(files.Length).IsEqualTo(1);
            await Assert.That(Path.GetDirectoryName(files[0])).IsEqualTo(outputDirectory);
        }
    }

    [Test]
    public async Task WriteAsync_FileNameContainsSessionId()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            Ulid sessionId = Ulid.NewUlid();
            await writer.WriteAsync(EmptyReport(sessionId));

            string[] files = Directory.GetFiles(outputDirectory, "*.md");
            await Assert
                .That(Path.GetFileName(files[0]).Contains(sessionId.ToString(), StringComparison.OrdinalIgnoreCase))
                .IsTrue();
        }
    }

    [Test]
    public async Task Constructor_DoesNotCreateConfiguredDirectory_UntilWriteAsync()
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), $"OT2.Reports.{Guid.NewGuid():N}");
        MarkdownRunReportWriter writer = new(
            NullLogger<MarkdownRunReportWriter>.Instance,
            new RunReportOptions { OutputDirectory = outputDirectory }
        );

        try
        {
            await Assert.That(Directory.Exists(outputDirectory)).IsFalse();

            await writer.WriteAsync(EmptyReport());

            await Assert.That(Directory.Exists(outputDirectory)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    // ── markdown content ──────────────────────────────────────────────────────

    [Test]
    public async Task WriteAsync_ContentContainsScenarioName()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            await writer.WriteAsync(EmptyReport());
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert.That(content.Contains("Training Ground", StringComparison.OrdinalIgnoreCase)).IsTrue();
        }
    }

    [Test]
    public async Task WriteAsync_ContentContainsDurationLine()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            await writer.WriteAsync(EmptyReport());
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert.That(content.Contains("**Duration:**", StringComparison.Ordinal)).IsTrue();
            // 5 min 30 s
            await Assert.That(content.Contains("5m 30s", StringComparison.Ordinal)).IsTrue();
        }
    }

    [Test]
    public async Task WriteAsync_ContentContainsSummaryStatisticsTable()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            await writer.WriteAsync(EmptyReport());
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert.That(content.Contains("## Summary Statistics", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("| Enemy Kills |", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("| Total Events Recorded |", StringComparison.Ordinal)).IsTrue();
        }
    }

    [Test]
    public async Task WriteAsync_ContentContainsPlayerJoinedEvent()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            DateTimeOffset start = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
            PlayerJoinedEvent joinEvent = new(
                start.AddSeconds(5),
                Ulid.NewUlid(),
                "Kevin",
                InitialHealth: 100,
                MaxHealth: 100,
                InitialVirusPercentage: 0.0
            );

            RunReport report = new(
                Ulid.NewUlid(),
                "training-ground",
                "Training Ground",
                Scenario.TrainingGround,
                start,
                start.AddMinutes(5),
                [joinEvent]
            );

            await writer.WriteAsync(report);
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert.That(content.Contains("**Kevin**", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("joined", StringComparison.OrdinalIgnoreCase)).IsTrue();
        }
    }

    [Test]
    public async Task WriteAsync_ContentContainsPrecisePlayerInventoryChange()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            DateTimeOffset start = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
            PlayerInventoryChangedEvent inventoryEvent = new(
                start.AddSeconds(5),
                Ulid.NewUlid(),
                "Kevin",
                InventoryKind.Main,
                SlotIndex: 0,
                OldItemId: 0x00,
                OldItemName: "Empty",
                NewItemId: 0x21,
                NewItemName: "Unknown"
            );

            RunReport report = new(
                Ulid.NewUlid(),
                "training-ground",
                "Training Ground",
                Scenario.TrainingGround,
                start,
                start.AddMinutes(5),
                [inventoryEvent]
            );

            await writer.WriteAsync(report);
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert.That(content.Contains("changed from Empty (0x00 | 0)", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("Unknown (0x21 | 33)", StringComparison.Ordinal)).IsTrue();
        }
    }

    [Test]
    public async Task WriteAsync_ContentContainsEnemyKilledEvent_WithContributors()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            DateTimeOffset start = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
            EnemyKilledEvent killEvent = new(
                start.AddSeconds(30),
                Ulid.NewUlid(),
                "Zombie",
                SlotId: 0,
                RoomId: 1,
                ContributingPlayers: [(Ulid.NewUlid(), "Kevin", 1.0f)]
            );

            RunReport report = new(
                Ulid.NewUlid(),
                "training-ground",
                "Training Ground",
                Scenario.TrainingGround,
                start,
                start.AddMinutes(5),
                [killEvent]
            );

            await writer.WriteAsync(report);
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert.That(content.Contains("**Zombie**", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("killed", StringComparison.OrdinalIgnoreCase)).IsTrue();
            await Assert.That(content.Contains("**Kevin**", StringComparison.Ordinal)).IsTrue();
        }
    }

    [Test]
    public async Task WriteAsync_MonsterLogSection_IsAbsent_WhenNoEnemyEvents()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            await writer.WriteAsync(EmptyReport());
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert.That(content.Contains("## Monster Log", StringComparison.Ordinal)).IsFalse();
        }
    }

    [Test]
    public async Task WriteAsync_MonsterLogSection_IsPresent_WhenEnemyEventsExist()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            DateTimeOffset start = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
            Ulid enemyId = Ulid.NewUlid();
            EnemySpawnedEvent spawnEvent = new(
                start.AddSeconds(5),
                enemyId,
                "Zombie",
                SlotId: 0,
                RoomId: 1,
                MaxHp: 200
            );
            EnemyKilledEvent killEvent = new(
                start.AddSeconds(30),
                enemyId,
                "Zombie",
                SlotId: 0,
                RoomId: 1,
                ContributingPlayers: []
            );

            RunReport report = new(
                Ulid.NewUlid(),
                "training-ground",
                "Training Ground",
                Scenario.TrainingGround,
                start,
                start.AddMinutes(5),
                [spawnEvent, killEvent]
            );

            await writer.WriteAsync(report);
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert.That(content.Contains("## Monster Log", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("Killed", StringComparison.Ordinal)).IsTrue();
        }
    }

    [Test]
    public async Task WriteAsync_SummaryStats_ReflectEventCounts()
    {
        (MarkdownRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            DateTimeOffset start = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
            Ulid playerId = Ulid.NewUlid();

            // 2 damage events → TotalDamageTaken = 60
            PlayerHealthChangedEvent hit1 = new(
                start.AddSeconds(10),
                playerId,
                "Kevin",
                OldHealth: 100,
                NewHealth: 70,
                MaxHealth: 100
            );
            PlayerHealthChangedEvent hit2 = new(
                start.AddSeconds(20),
                playerId,
                "Kevin",
                OldHealth: 70,
                NewHealth: 40,
                MaxHealth: 100
            );

            RunReport report = new(
                Ulid.NewUlid(),
                "training-ground",
                "Training Ground",
                Scenario.TrainingGround,
                start,
                start.AddMinutes(5),
                [hit1, hit2]
            );

            await writer.WriteAsync(report);
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert
                .That(content.Contains("| Total Damage Taken (all players) | 60 HP |", StringComparison.Ordinal))
                .IsTrue();
        }
    }

    // ── TempDirectoryCleanup ─────────────────────────────────────────────────

    private sealed class TempDirectoryCleanup(string path) : IDisposable
    {
        public void Dispose()
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }
}

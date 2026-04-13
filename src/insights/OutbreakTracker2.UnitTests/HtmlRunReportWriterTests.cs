using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Reports;
using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.UnitTests;

public sealed class HtmlRunReportWriterTests
{
    private static (HtmlRunReportWriter Writer, string OutputDirectory, IDisposable Cleanup) CreateWriterWithTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"OT2.HtmlReports.{Guid.NewGuid():N}");
        HtmlRunReportWriter writer = new(
            NullLogger<HtmlRunReportWriter>.Instance,
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
        string[] files = Directory.GetFiles(directory, "*.html");
        return await File.ReadAllTextAsync(files[0]);
    }

    [Test]
    public async Task WriteAsync_UsesConfiguredOutputDirectory()
    {
        (HtmlRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            await writer.WriteAsync(EmptyReport());

            string[] files = Directory.GetFiles(outputDirectory, "*.html");
            await Assert.That(files.Length).IsEqualTo(1);
            await Assert.That(Path.GetDirectoryName(files[0])).IsEqualTo(outputDirectory);
        }
    }

    [Test]
    public async Task WriteAsync_FileNameContainsSessionId()
    {
        (HtmlRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            Ulid sessionId = Ulid.NewUlid();
            await writer.WriteAsync(EmptyReport(sessionId));

            string[] files = Directory.GetFiles(outputDirectory, "*.html");
            await Assert
                .That(Path.GetFileName(files[0]).Contains(sessionId.ToString(), StringComparison.OrdinalIgnoreCase))
                .IsTrue();
        }
    }

    [Test]
    public async Task WriteAsync_ContentContainsInteractiveShell()
    {
        (HtmlRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
        using (cleanup)
        {
            await writer.WriteAsync(EmptyReport());
            string content = await ReadSingleFileAsync(outputDirectory);

            await Assert.That(content.Contains("Scenario Run Report", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("id=\"eventHistogram\"", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("type=\"application/json\"", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("data-group=\"actor\"", StringComparison.Ordinal)).IsTrue();
        }
    }

    [Test]
    public async Task WriteAsync_ContentContainsEventDescriptionAndContributors()
    {
        (HtmlRunReportWriter writer, string outputDirectory, IDisposable cleanup) = CreateWriterWithTempDir();
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

            await Assert.That(content.Contains("Zombie", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("Kevin", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("Copy link", StringComparison.Ordinal)).IsTrue();
            await Assert.That(content.Contains("tracked events", StringComparison.Ordinal)).IsTrue();
        }
    }

    private sealed class TempDirectoryCleanup(string path) : IDisposable
    {
        public void Dispose()
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }
}

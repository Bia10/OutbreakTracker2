using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Reports;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.UnitTests;

public sealed class CompositeRunReportWriterTests
{
    private static RunReport CreateReport() =>
        new(
            Ulid.NewUlid(),
            "training-ground",
            "Training Ground",
            Scenario.TrainingGround,
            new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 15, 10, 5, 30, TimeSpan.Zero),
            []
        );

    [Test]
    public async Task WriteAsync_WritesHtmlAndMarkdownArtifacts_WhenEnabled()
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), $"OT2.CompositeReports.{Guid.NewGuid():N}");
        RunReportOptions options = new()
        {
            OutputDirectory = outputDirectory,
            WriteMarkdown = true,
            WriteHtml = true,
        };
        using TempDirectoryCleanup cleanup = new(outputDirectory);

        CompositeRunReportWriter writer = new(
            new MarkdownRunReportWriter(NullLogger<MarkdownRunReportWriter>.Instance, options),
            new HtmlRunReportWriter(NullLogger<HtmlRunReportWriter>.Instance, options),
            options,
            NullLogger<CompositeRunReportWriter>.Instance
        );

        await writer.WriteAsync(CreateReport());

        await Assert.That(Directory.GetFiles(outputDirectory, "*.md").Length).IsEqualTo(1);
        await Assert.That(Directory.GetFiles(outputDirectory, "*.html").Length).IsEqualTo(1);
    }

    [Test]
    public async Task WriteAsync_WritesOnlyHtmlArtifact_WhenMarkdownDisabled()
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), $"OT2.CompositeReports.{Guid.NewGuid():N}");
        RunReportOptions options = new()
        {
            OutputDirectory = outputDirectory,
            WriteMarkdown = false,
            WriteHtml = true,
        };
        using TempDirectoryCleanup cleanup = new(outputDirectory);

        CompositeRunReportWriter writer = new(
            new MarkdownRunReportWriter(NullLogger<MarkdownRunReportWriter>.Instance, options),
            new HtmlRunReportWriter(NullLogger<HtmlRunReportWriter>.Instance, options),
            options,
            NullLogger<CompositeRunReportWriter>.Instance
        );

        await writer.WriteAsync(CreateReport());

        await Assert.That(Directory.GetFiles(outputDirectory, "*.md").Length).IsEqualTo(0);
        await Assert.That(Directory.GetFiles(outputDirectory, "*.html").Length).IsEqualTo(1);
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

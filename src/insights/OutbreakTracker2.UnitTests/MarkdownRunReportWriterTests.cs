using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Reports;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.UnitTests;

public sealed class MarkdownRunReportWriterTests
{
    [Test]
    public async Task WriteAsync_UsesConfiguredOutputDirectory()
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), $"OutbreakTracker2.RunReports.{Guid.NewGuid():N}");

        try
        {
            MarkdownRunReportWriter writer = new(
                NullLogger<MarkdownRunReportWriter>.Instance,
                new RunReportOptions { OutputDirectory = outputDirectory }
            );

            RunReport report = new(
                Ulid.NewUlid(),
                "training-ground",
                "Training ground",
                Scenario.TrainingGround,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(5),
                []
            );

            await writer.WriteAsync(report);

            string[] files = Directory.GetFiles(outputDirectory, "*.md");
            await Assert.That(files.Length).IsEqualTo(1);
            await Assert.That(Path.GetDirectoryName(files[0])).IsEqualTo(outputDirectory);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }
}

using System.Globalization;
using Microsoft.Extensions.Logging;
using nietras.SeparatedValues;
using OutbreakTracker2.Application.Services.Reports.Events;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed class CsvRunReportWriter : IRunReportWriter
{
    private readonly string _outputDirectory;
    private readonly ILogger<CsvRunReportWriter> _logger;

    public CsvRunReportWriter(RunReportOptions options, ILogger<CsvRunReportWriter> logger)
    {
        _outputDirectory = RunReportOutputPathUtility.ResolveOutputDirectory(options);
        _logger = logger;
    }

    public Task WriteAsync(RunReport report, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_outputDirectory);
        string filePath = Path.Combine(_outputDirectory, RunReportOutputPathUtility.GetFileName(report, "csv"));

        WriteCsv(report, filePath);

        _logger.LogInformation(
            "CSV run report written to {FilePath} for session {SessionId}",
            filePath,
            report.SessionId
        );

        return Task.CompletedTask;
    }

    private static void WriteCsv(RunReport report, string filePath)
    {
        using SepWriter writer = Sep.New(',').Writer(o => o with { Escape = true }).ToFile(filePath);

        string sessionId = report.SessionId.ToString(null, CultureInfo.InvariantCulture);
        string scenarioId = report.ScenarioId;
        string scenarioName = report.ScenarioName;
        string scenario =
            Enum.GetName(report.Scenario) ?? ((int)report.Scenario).ToString(CultureInfo.InvariantCulture);
        string sessionStart = report.StartedAt.ToString("O", CultureInfo.InvariantCulture);
        string sessionEnd = report.EndedAt.ToString("O", CultureInfo.InvariantCulture);
        double durationSeconds = report.Duration.TotalSeconds;

        foreach (RunEvent evt in report.Events)
        {
            using SepWriter.Row row = writer.NewRow();
            row["SessionId"].Set(sessionId);
            row["ScenarioId"].Set(scenarioId);
            row["ScenarioName"].Set(scenarioName);
            row["Scenario"].Set(scenario);
            row["SessionStart"].Set(sessionStart);
            row["SessionEnd"].Set(sessionEnd);
            row["DurationSeconds"].Format(durationSeconds);
            row["ScenarioFrame"].Format(evt.ScenarioFrame);
            row["OccurredAt"].Set(evt.OccurredAt.ToString("O", CultureInfo.InvariantCulture));
            row["EventType"].Set(evt.GetType().Name);
            row["Description"].Set(evt.Describe(report.Scenario));
        }
    }
}

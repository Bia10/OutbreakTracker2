namespace OutbreakTracker2.Application.Services.Settings;

public sealed record RunReportSettings
{
    public bool GenerateRunReports { get; init; } = true;

    public bool WriteMarkdown { get; init; } = true;

    public bool WriteCsv { get; init; } = true;

    public bool WriteHtml { get; init; } = true;
}

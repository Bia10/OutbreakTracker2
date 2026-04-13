namespace OutbreakTracker2.Application.Services.Reports;

public sealed record RunReportOptions
{
    public const string SectionName = "RunReports";
    public const string DefaultOutputDirectory = "reports";

    public string OutputDirectory { get; init; } = DefaultOutputDirectory;

    public bool WriteMarkdown { get; init; } = true;

    public bool WriteHtml { get; init; } = true;
}

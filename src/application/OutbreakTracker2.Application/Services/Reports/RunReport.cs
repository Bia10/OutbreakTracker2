using OutbreakTracker2.Application.Services.Reports.Events;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed record RunReport(
    Ulid SessionId,
    string ScenarioId,
    string ScenarioName,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    IReadOnlyList<RunEvent> Events
)
{
    public TimeSpan Duration => EndedAt - StartedAt;

    public RunReportStats ComputeStats() => RunReportStats.From(this);
}

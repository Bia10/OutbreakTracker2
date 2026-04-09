using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed record RunReport(
    Ulid SessionId,
    string ScenarioId,
    string ScenarioName,
    Scenario Scenario,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    IReadOnlyList<RunEvent> Events
)
{
    public TimeSpan Duration => EndedAt - StartedAt;

    public RunReportStats ComputeStats() => RunReportStats.From(this);
}

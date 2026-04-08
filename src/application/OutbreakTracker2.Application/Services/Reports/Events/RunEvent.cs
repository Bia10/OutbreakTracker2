namespace OutbreakTracker2.Application.Services.Reports.Events;

public abstract record RunEvent(DateTimeOffset OccurredAt)
{
    public int ScenarioFrame { get; init; }
}

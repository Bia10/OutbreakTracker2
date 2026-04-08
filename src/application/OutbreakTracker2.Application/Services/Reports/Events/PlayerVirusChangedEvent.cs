namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerVirusChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    double OldVirusPercentage,
    double NewVirusPercentage
) : RunEvent(OccurredAt)
{
    public double Delta => NewVirusPercentage - OldVirusPercentage;
}

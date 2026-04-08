namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerLeftEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    short FinalHealth,
    double FinalVirusPercentage
) : RunEvent(OccurredAt);

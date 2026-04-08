namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerJoinedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    short InitialHealth,
    short MaxHealth,
    double InitialVirusPercentage
) : RunEvent(OccurredAt);

using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerLeftEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    short FinalHealth,
    double FinalVirusPercentage
) : RunEvent(OccurredAt)
{
    public override string Describe(Scenario scenario) =>
        Invariant($"Player **{PlayerName}** left (HP: {FinalHealth}, Virus: {FinalVirusPercentage:F3}%)");
}

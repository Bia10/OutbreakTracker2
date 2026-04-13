using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerJoinedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    short InitialHealth,
    short MaxHealth,
    double InitialVirusPercentage
) : RunEvent(OccurredAt)
{
    public override string Describe(Scenario scenario) =>
        Invariant(
            $"Player **{PlayerName}** joined (HP: {InitialHealth}/{MaxHealth}, Virus: {InitialVirusPercentage:F3}%)"
        );

    internal override void Accumulate(IRunEventStatsAccumulator accumulator) => accumulator.Accumulate(this);
}

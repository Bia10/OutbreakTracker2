using OutbreakTracker2.Outbreak.Enums;

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

    public override string Describe(Scenario scenario) =>
        Delta > 0
            ? Invariant(
                $"Player **{PlayerName}** virus: {OldVirusPercentage:F3}% → **{NewVirusPercentage:F3}%** (+{Delta:F3}%)"
            )
            : Invariant(
                $"Player **{PlayerName}** virus: {OldVirusPercentage:F3}% → **{NewVirusPercentage:F3}%** ({Delta:F3}%)"
            );
}

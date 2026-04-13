namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record EnemyKilledEvent(
    DateTimeOffset OccurredAt,
    Ulid EnemyId,
    string EnemyName,
    short SlotId,
    byte RoomId,
    IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> ContributingPlayers
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant(
            $"Enemy **{EnemyName}** killed ({RoomName(scenario, RoomId)}, Slot {SlotId}){FormatContributions(ContributingPlayers)}"
        );
}

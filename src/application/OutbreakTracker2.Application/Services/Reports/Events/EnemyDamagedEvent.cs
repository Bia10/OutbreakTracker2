namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record EnemyDamagedEvent(
    DateTimeOffset OccurredAt,
    Ulid EnemyId,
    string EnemyName,
    short SlotId,
    byte RoomId,
    ushort OldHp,
    ushort NewHp,
    ushort MaxHp,
    IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> ContributingPlayers
) : RunEvent(OccurredAt)
{
    public ushort Damage => (ushort)(OldHp - NewHp);

    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant(
            $"Enemy **{EnemyName}** damaged: {OldHp} → {NewHp}/{MaxHp} (-{Damage}) ({RoomName(scenario, RoomId)}){FormatContributions(ContributingPlayers)}"
        );
}

namespace OutbreakTracker2.Application.Services.Reports.Events;

/// <summary>
/// Emitted when a player's status changes (e.g. OK → Poison, Bleed → OK).
/// </summary>
public sealed record PlayerStatusChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    string OldStatus,
    string NewStatus
) : RunEvent(OccurredAt)
{
    public override string Describe(OutbreakTracker2.Outbreak.Enums.Scenario scenario) =>
        Invariant($"Player **{PlayerName}** status: {OldStatus} → **{NewStatus}**");
}

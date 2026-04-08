namespace OutbreakTracker2.Application.Services.Reports.Events;

/// <summary>
/// Emitted when an enemy's AI status byte changes (e.g. inactive → active/pursuing).
/// <para>
/// The <see cref="OldStatus"/> and <see cref="NewStatus"/> are raw bytes read directly from
/// game memory — no enum mapping exists yet. Value 0x00 typically represents inactive/idle.
/// </para>
/// </summary>
public sealed record EnemyStatusChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid EnemyId,
    string EnemyName,
    short SlotId,
    byte RoomId,
    byte OldStatus,
    byte NewStatus,
    IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> ContributingPlayers
) : RunEvent(OccurredAt)
{
    /// <summary>True when the status changed from 0x00 (inactive/idle) to a non-zero value.</summary>
    public bool IsActivation => OldStatus == 0x00 && NewStatus != 0x00;
}

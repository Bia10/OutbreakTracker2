using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Application.Services.Reports.Events;

public sealed record PlayerHealthChangedEvent(
    DateTimeOffset OccurredAt,
    Ulid PlayerId,
    string PlayerName,
    short OldHealth,
    short NewHealth,
    short MaxHealth
) : RunEvent(OccurredAt)
{
    public short Delta => (short)(NewHealth - OldHealth);
    public bool IsDamage => NewHealth < OldHealth;
    public bool IsHeal => NewHealth > OldHealth;

    public override string Describe(Scenario scenario)
    {
        if (IsDamage)
            return Invariant(
                $"Player **{PlayerName}** took **{OldHealth - NewHealth} damage** ({OldHealth} → {NewHealth}/{MaxHealth})"
            );

        if (IsHeal)
            return Invariant(
                $"Player **{PlayerName}** healed **+{NewHealth - OldHealth} HP** ({OldHealth} → {NewHealth}/{MaxHealth})"
            );

        return Invariant($"Player **{PlayerName}** health: {OldHealth} → **{NewHealth}/{MaxHealth}**");
    }
}

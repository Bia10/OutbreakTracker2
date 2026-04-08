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
}

namespace OutbreakTracker2.Application.Services.Settings;

public sealed record NotificationSettings
{
    public bool EnableToastAlerts { get; init; } = true;
}

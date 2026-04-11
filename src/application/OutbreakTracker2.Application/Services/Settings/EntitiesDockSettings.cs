namespace OutbreakTracker2.Application.Services.Settings;

public sealed record EntitiesDockSettings
{
    public bool OnlyShowCurrentPlayerRoom { get; init; } = true;
}

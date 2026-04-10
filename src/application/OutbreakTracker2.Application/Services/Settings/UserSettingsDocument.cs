using System.Text.Json.Serialization;

namespace OutbreakTracker2.Application.Services.Settings;

internal sealed record UserSettingsDocument
{
    [JsonPropertyName(OutbreakTrackerSettings.SectionName)]
    public OutbreakTrackerSettings OutbreakTracker { get; init; } = new();
}

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.Application.Services.Settings;

internal sealed record UserSettingsDocument
{
    [JsonPropertyName(OutbreakTrackerSettings.SectionName)]
    public OutbreakTrackerSettings? OutbreakTracker { get; init; }

    public bool TryValidate([NotNullWhen(false)] out string? error)
    {
        if (OutbreakTracker is null)
        {
            error = "The user settings file must contain an OutbreakTracker object.";
            return false;
        }

        return OutbreakTracker.TryValidate(out error);
    }
}

using System.Text.Json;

namespace OutbreakTracker2.Application.Services.Settings;

internal interface ISettingsSerializer
{
    ValueTask SerializeAsync(Stream destination, OutbreakTrackerSettings settings, CancellationToken cancellationToken);

    OutbreakTrackerSettings DeserializeSettings(JsonElement settingsElement);

    bool TryGetSettingsSection(JsonElement root, out JsonElement settingsSection);
}

using System.Text.Json;

namespace OutbreakTracker2.Application.Services.Settings;

internal sealed class SettingsJsonSerializer : ISettingsSerializer
{
    public async ValueTask SerializeAsync(
        Stream destination,
        OutbreakTrackerSettings settings,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (destination.CanSeek)
        {
            destination.SetLength(0);
            destination.Position = 0;
        }

        UserSettingsDocument document = new() { OutbreakTracker = settings };

        await JsonSerializer
            .SerializeAsync(destination, document, SettingsJsonContext.Default.UserSettingsDocument, cancellationToken)
            .ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public OutbreakTrackerSettings DeserializeSettings(JsonElement settingsElement) =>
        settingsElement.Deserialize(SettingsJsonContext.Default.OutbreakTrackerSettings)
        ?? throw new InvalidOperationException("The settings document does not contain valid tracker settings.");

    public bool TryGetSettingsSection(JsonElement root, out JsonElement settingsSection)
    {
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, OutbreakTrackerSettings.SectionName, StringComparison.OrdinalIgnoreCase))
                continue;

            settingsSection = property.Value;
            return true;
        }

        settingsSection = default;
        return false;
    }
}

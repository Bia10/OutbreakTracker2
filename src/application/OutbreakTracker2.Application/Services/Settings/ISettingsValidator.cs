using System.Text.Json;

namespace OutbreakTracker2.Application.Services.Settings;

internal interface ISettingsValidator
{
    void ValidateSettings(OutbreakTrackerSettings settings);

    void ValidateOverridesElement(JsonElement settingsElement, string rootPath);
}

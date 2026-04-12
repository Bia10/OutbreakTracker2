using System.Text.Json;

namespace OutbreakTracker2.Application.Services.Settings;

internal sealed class SettingsValidator : ISettingsValidator
{
    public void ValidateSettings(OutbreakTrackerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.TryValidate(out string? error))
            throw new InvalidOperationException(error);
    }

    public void ValidateOverridesElement(JsonElement settingsElement, string rootPath)
    {
        if (settingsElement.ValueKind == JsonValueKind.Null)
            throw new InvalidOperationException($"{rootPath} cannot be null.");

        if (settingsElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"{rootPath} must be a JSON object.");

        ValidateOptionalObjectProperty(settingsElement, "Notifications", $"{rootPath}:Notifications");
        if (TryGetPropertyIgnoreCase(settingsElement, "Display", out JsonElement displayElement))
        {
            ValidateObjectValue(displayElement, $"{rootPath}:Display");
            ValidateOptionalObjectProperty(displayElement, "EntitiesDock", $"{rootPath}:Display:EntitiesDock");
            ValidateOptionalObjectProperty(
                displayElement,
                "ScenarioItemsDock",
                $"{rootPath}:Display:ScenarioItemsDock"
            );
        }

        if (TryGetPropertyIgnoreCase(settingsElement, "AlertRules", out JsonElement alertRulesElement))
        {
            ValidateObjectValue(alertRulesElement, $"{rootPath}:AlertRules");
            ValidateOptionalObjectProperty(alertRulesElement, "Players", $"{rootPath}:AlertRules:Players");
            ValidateOptionalObjectProperty(alertRulesElement, "Enemies", $"{rootPath}:AlertRules:Enemies");
            ValidateOptionalObjectProperty(alertRulesElement, "Doors", $"{rootPath}:AlertRules:Doors");
            ValidateOptionalObjectProperty(alertRulesElement, "Lobby", $"{rootPath}:AlertRules:Lobby");
        }
    }

    private static void ValidateOptionalObjectProperty(JsonElement parent, string propertyName, string propertyPath)
    {
        if (!TryGetPropertyIgnoreCase(parent, propertyName, out JsonElement propertyValue))
            return;

        ValidateObjectValue(propertyValue, propertyPath);
    }

    private static void ValidateObjectValue(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Null)
            throw new InvalidOperationException($"{path} cannot be null.");

        if (value.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"{path} must be a JSON object.");
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement propertyValue
    )
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                continue;

            propertyValue = property.Value;
            return true;
        }

        propertyValue = default;
        return false;
    }
}

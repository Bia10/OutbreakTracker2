using System.Text.Json.Serialization;

namespace OutbreakTracker2.Application.Services.Settings;

[JsonSerializable(typeof(UserSettingsDocument))]
[JsonSerializable(typeof(OutbreakTrackerSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

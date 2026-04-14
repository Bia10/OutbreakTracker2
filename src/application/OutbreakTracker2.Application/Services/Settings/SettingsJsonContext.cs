using System.Text.Json.Serialization;

namespace OutbreakTracker2.Application.Services.Settings;

[JsonSerializable(typeof(UserSettingsDocument))]
[JsonSerializable(typeof(OutbreakTrackerSettings))]
[JsonSerializable(typeof(RunReportSettings))]
[JsonSerializable(typeof(DataManagerSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

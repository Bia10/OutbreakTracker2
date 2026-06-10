using System.Text.Json.Serialization;
using OutbreakTracker2.MemoryWatcherIntegration;

namespace OutbreakTracker2.Application.Services.Settings;

[JsonSerializable(typeof(UserSettingsDocument))]
[JsonSerializable(typeof(OutbreakTrackerSettings))]
[JsonSerializable(typeof(RunReportSettings))]
[JsonSerializable(typeof(DataManagerSettings))]
[JsonSerializable(typeof(MemoryWatcherSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

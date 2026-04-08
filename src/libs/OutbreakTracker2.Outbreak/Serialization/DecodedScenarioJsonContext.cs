using System.Text.Json.Serialization;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Serialization;

[JsonSerializable(typeof(DecodedInGameScenario))]
[JsonSerializable(typeof(ScenarioStatus))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class DecodedScenarioJsonContext : JsonSerializerContext;

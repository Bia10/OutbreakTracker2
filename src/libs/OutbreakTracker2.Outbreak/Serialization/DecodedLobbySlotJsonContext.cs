using OutbreakTracker2.Outbreak.Models;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Serialization;

[JsonSerializable(typeof(DecodedLobbySlot))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class DecodedLobbySlotJsonContext : JsonSerializerContext;

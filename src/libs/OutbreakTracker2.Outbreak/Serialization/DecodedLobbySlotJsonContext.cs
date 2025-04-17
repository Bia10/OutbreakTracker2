using System.Text.Json.Serialization;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Serialization;

[JsonSerializable(typeof(DecodedLobbySlot))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class DecodedLobbySlotJsonContext : JsonSerializerContext;

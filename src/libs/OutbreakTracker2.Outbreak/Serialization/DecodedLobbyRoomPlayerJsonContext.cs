using OutbreakTracker2.Outbreak.Models;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Serialization;

[JsonSerializable(typeof(DecodedLobbyRoomPlayer))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class DecodedLobbyRoomPlayerJsonContext : JsonSerializerContext;

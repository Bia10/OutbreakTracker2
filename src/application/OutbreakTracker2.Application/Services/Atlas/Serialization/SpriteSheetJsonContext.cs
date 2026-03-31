using System.Text.Json.Serialization;
using OutbreakTracker2.Application.Services.Atlas.Models;

namespace OutbreakTracker2.Application.Services.Atlas.Serialization;

[JsonSerializable(typeof(SpriteSheet))]
[JsonSerializable(typeof(List<Frame>))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public sealed partial class SpriteSheetJsonContext : JsonSerializerContext;

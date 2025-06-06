using OutbreakTracker2.Application.Services.Atlas.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.Application.Services.Atlas.Serialization;

[JsonSerializable(typeof(SpriteSheet))]
[JsonSerializable(typeof(List<Frame>))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public sealed partial class SpriteSheetJsonContext : JsonSerializerContext;
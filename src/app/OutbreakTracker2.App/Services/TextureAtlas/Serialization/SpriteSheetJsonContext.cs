using OutbreakTracker2.App.Services.TextureAtlas.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.App.Services.TextureAtlas.Serialization;

[JsonSerializable(typeof(SpriteSheet))]
[JsonSerializable(typeof(List<Frame>))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public sealed partial class SpriteSheetJsonContext : JsonSerializerContext;
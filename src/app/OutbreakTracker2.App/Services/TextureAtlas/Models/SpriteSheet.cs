using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.App.Services.TextureAtlas.Models;

public sealed class SpriteSheet
{
    public List<Frame> Frames { get; set; } = [];

    public int SheetContentWidth { get; set; }

    public int SheetContentHeight { get; set; }

    [JsonIgnore]
    public Dictionary<string, Frame> FrameLookup { get; private set; } = [];

    /// <summary>
    /// Builds a lookup dictionary for frames by their name.
    /// This should be called after deserialization.
    /// </summary>
    public void BuildFrameLookup()
        => FrameLookup = Frames.ToDictionary(frame => frame.Name, frame => frame);
}
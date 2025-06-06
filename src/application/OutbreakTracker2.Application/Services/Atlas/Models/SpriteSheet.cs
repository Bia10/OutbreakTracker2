using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.Application.Services.Atlas.Models;

public sealed class SpriteSheet
{
    public IReadOnlyList<Frame> Frames { get; set; } = [];

    public int SheetContentWidth { get; set; }

    public int SheetContentHeight { get; set; }

    [JsonIgnore]
    public IReadOnlyDictionary<string, Frame> FrameLookup { get; private set; }
        = new Dictionary<string, Frame>(System.StringComparer.Ordinal);

    /// <summary>
    /// Builds a lookup dictionary for frames by their name.
    /// This should be called after deserialization.
    /// </summary>
    public void BuildFrameLookup()
        => FrameLookup = Frames.ToDictionary(frame => frame.Name, frame => frame, System.StringComparer.Ordinal);
}
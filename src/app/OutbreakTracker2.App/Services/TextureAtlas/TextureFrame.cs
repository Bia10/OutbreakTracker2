using Avalonia;

namespace OutbreakTracker2.App.Services.TextureAtlas;

public sealed class TextureFrame
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    /// <summary>
    /// Converts the frame's coordinates and dimensions into an Avalonia.Rect.
    /// This is a utility method; the core TextureFrame is UI-agnostic.
    /// </summary>
    public Rect ToRect()
        => new(X, Y, Width, Height);
}
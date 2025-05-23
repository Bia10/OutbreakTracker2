using Avalonia;

namespace OutbreakTracker2.App.Services.TextureAtlas.Models;

public sealed class Frame
{
    public string Name { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>
    /// Converts the frame's coordinates and dimensions into an Avalonia.Rect.
    /// This is a utility method; the core TextureFrame is UI-agnostic.
    /// </summary>
    public Rect ToRect() => new(X, Y, Width, Height);
}
using Avalonia;
using Avalonia.Media.Imaging;

namespace OutbreakTracker2.App.Services.TextureAtlas;

public interface ITextureAtlas
{
    /// <summary>
    /// Gets the full texture atlas bitmap.
    /// </summary>
    Bitmap? Texture { get; }

    /// <summary>
    /// Gets the source rectangle (quad) for a named texture within the atlas.
    /// </summary>
    /// <param name="name">The name of the texture (e.g., "bustAlyssa").</param>
    /// <returns>An Avalonia.Rect representing the source region on the atlas, or Rect.Empty if the name is not found.</returns>
    Rect GetSourceRectangle(string name);
}
using Avalonia;
using Avalonia.Media.Imaging;

namespace OutbreakTracker2.Application.Services.Atlas;

public interface ITextureAtlas
{
    /// <summary>
    /// Gets the full texture atlas bitmap.
    /// </summary>
    Bitmap? Texture { get; }

    /// <summary>
    /// Attempts to get the source rectangle (quad) for a named texture within the atlas.
    /// </summary>
    /// <param name="name">The name of the texture (e.g., "bustAlyssa").</param>
    /// <param name="rect">The resolved source rectangle when the lookup succeeds.</param>
    /// <returns><c>true</c> when the sprite exists in the atlas; otherwise <c>false</c>.</returns>
    bool TryGetSourceRectangle(string name, out Rect rect);

    /// <summary>
    /// Gets the source rectangle (quad) for a named texture within the atlas.
    /// </summary>
    /// <param name="name">The name of the texture (e.g., "bustAlyssa").</param>
    /// <returns>An Avalonia.Rect representing the source region on the atlas.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the atlas metadata is unavailable.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the sprite name does not exist in the atlas.</exception>
    Rect GetSourceRectangle(string name);
}

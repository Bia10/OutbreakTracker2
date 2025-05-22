using Avalonia;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;

namespace OutbreakTracker2.App.Services.TextureAtlas;

public class TextureAtlas : ITextureAtlas, IDisposable
{
    private TextureAtlasInfo? _info;
    private Dictionary<string, Rect>? _quads;

    public TextureAtlas(Stream? imageStream, TextureAtlasInfo? info)
    {
        if (imageStream is null)
            throw new ArgumentNullException(nameof(imageStream), "Image stream cannot be null.");

        Texture = new Bitmap(imageStream);
        _info = info ?? throw new ArgumentNullException(nameof(info), "TextureAtlasInfo cannot be null.");
        _quads = new Dictionary<string, Rect>(StringComparer.OrdinalIgnoreCase);

        foreach ((string name, int frameIndex) in _info.FrameIndex)
        {
            if (frameIndex >= 0 && frameIndex < _info.Frames.Count)
                _quads[name] = _info.Frames[frameIndex]
                    .ToRect();
            else
                Console.WriteLine(
                    $"Warning: Frame index {frameIndex} for sprite '{name}' is out of bounds in TextureAtlasInfo.Frames.");
        }
    }

    private static Lazy<TextureAtlas>? _lazyInstance;

    /// <summary>
    /// Gets the singleton instance of the TextureAtlas.
    /// This property will trigger the loading of the texture atlas if it hasn't been loaded yet.
    /// </summary>
    public static TextureAtlas Instance
    {
        get
        {
            if (_lazyInstance is null)
                throw new InvalidOperationException("TextureAtlas has not been initialized. Call TextureAtlas.Initialize() first.");

            return _lazyInstance.Value;
        }
    }

    /// <summary>
    /// Initializes the TextureAtlas for lazy loading. This method should be called once at application startup.
    /// </summary>
    /// <param name="imageStream">The stream containing the texture atlas image data.</param>
    /// <param name="info">The TextureAtlasInfo object containing frame definitions.</param>
    public static void Initialize(Stream? imageStream, TextureAtlasInfo? info)
    {
        if (_lazyInstance is not null)
        {
            Console.WriteLine("Warning: TextureAtlas.Initialize() called more than once. Ignoring subsequent calls.");
            return;
        }

        _lazyInstance = new Lazy<TextureAtlas>(() => new TextureAtlas(imageStream, info));
    }

    /// <summary>
    /// Gets the underlying Avalonia.Media.Imaging.Bitmap texture.
    /// This is useful if you need to pass the entire atlas bitmap to a control that accepts an ImageSource.
    /// </summary>
    public Bitmap? Texture { get; private set; }

    /// <summary>
    /// Gets the source rectangle (quad) for a named texture within the atlas.
    /// This rectangle can be used with Avalonia's DrawingContext.DrawImage or
    /// to create a CroppedBitmap for individual sprite display.
    /// </summary>
    /// <param name="name">The name of the texture (e.g., "bustAlyssa").</param>
    /// <returns>An Avalonia.Rect representing the source region on the atlas, or Rect.Empty if the name is not found.</returns>
    public Rect GetSourceRectangle(string name)
    {
        if (_info is not null && _info.FrameIndex.TryGetValue(name, out int frameIndex))
        {
            if (frameIndex >= 0 && frameIndex < _info.Frames.Count)
                return _info.Frames[frameIndex]
                    .ToRect();

            Console.WriteLine($"Warning: Frame index {frameIndex} for sprite '{name}' is out of bounds in TextureAtlasInfo.Frames.");
            return new Rect();
        }

        Console.WriteLine($"Warning: Texture '{name}' not found in atlas. Returning empty rectangle.");
        return new Rect();
    }

    /// <summary>
    /// Disposes of the managed resources used by the TextureAtlas, specifically the Bitmap.
    /// This method should be called when the application is shutting down.
    /// </summary>
    public void Dispose()
    {
        Texture?.Dispose();

        Texture = null;
        _info = null;
        _quads = null;
    }
}
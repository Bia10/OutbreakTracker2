using Avalonia;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas.Models;

namespace OutbreakTracker2.Application.Services.Atlas;

public sealed class TextureAtlas : ITextureAtlas, IDisposable
{
    private readonly ILogger<TextureAtlas> _logger;
    private SpriteSheet? _spriteSheet;

    public TextureAtlas(Stream imageStream, SpriteSheet spriteSheet, ILogger<TextureAtlas> logger)
    {
        if (imageStream is null)
            throw new ArgumentNullException(nameof(imageStream), "Image stream cannot be null.");

        Texture = new Bitmap(imageStream);
        _spriteSheet =
            spriteSheet ?? throw new ArgumentNullException(nameof(spriteSheet), "SpriteSheet cannot be null.");
        _logger = logger;
    }

    public Bitmap? Texture { get; private set; }

    public bool TryGetSourceRectangle(string name, out Rect rect)
    {
        if (_spriteSheet is null)
        {
            _logger.LogWarning("SpriteSheet is null. Cannot get source rectangle for '{Name}'", name);
            rect = default;
            return false;
        }

        if (_spriteSheet.FrameLookup.Count == 0)
        {
            _logger.LogWarning("FrameLookup is empty. Cannot get source rectangle for '{Name}'", name);
            rect = default;
            return false;
        }

        if (_spriteSheet.FrameLookup.TryGetValue(name, out Frame? frame))
        {
            rect = frame.ToRect();
            return true;
        }

        _logger.LogWarning("Frame '{Name}' not found in FrameLookup", name);
        rect = default;
        return false;
    }

    public Rect GetSourceRectangle(string name)
    {
        if (_spriteSheet is null)
            throw new InvalidOperationException(
                "Texture atlas is unavailable because the sprite sheet has been released."
            );

        if (_spriteSheet.FrameLookup.Count == 0)
            throw new InvalidOperationException("Texture atlas does not contain any frame metadata.");

        if (_spriteSheet.FrameLookup.TryGetValue(name, out Frame? frame))
            return frame.ToRect();

        throw new KeyNotFoundException($"Frame '{name}' was not found in the texture atlas.");
    }

    public void Dispose()
    {
        Texture?.Dispose();
        Texture = null;
        _spriteSheet = null;
    }
}

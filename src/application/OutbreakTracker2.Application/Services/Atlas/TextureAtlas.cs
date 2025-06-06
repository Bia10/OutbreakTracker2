using Avalonia;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas.Models;
using System;
using System.IO;

namespace OutbreakTracker2.Application.Services.Atlas;

public class TextureAtlas : ITextureAtlas, IDisposable
{
    private readonly ILogger<TextureAtlas> _logger;
    private SpriteSheet? _spriteSheet;

    public TextureAtlas(Stream imageStream, SpriteSheet spriteSheet, ILogger<TextureAtlas> logger)
    {
        if (imageStream is null)
            throw new ArgumentNullException(nameof(imageStream), "Image stream cannot be null.");

        Texture = new Bitmap(imageStream);
        _spriteSheet = spriteSheet ?? throw new ArgumentNullException(nameof(spriteSheet), "SpriteSheet cannot be null.");
        _logger = logger;
    }

    public Bitmap? Texture { get; private set; }

    public Rect GetSourceRectangle(string name)
    {
        if (_spriteSheet is null)
        {
            _logger.LogWarning("SpriteSheet is null. Cannot get source rectangle for '{Name}'", name);
            return new Rect();
        }

        if (_spriteSheet.FrameLookup.Count == 0)
        {
            _logger.LogWarning("FrameLookup is empty. Cannot get source rectangle for '{Name}'", name);
            return new Rect();
        }

        if (_spriteSheet.FrameLookup.TryGetValue(name, out Frame? frame))
            return frame.ToRect();

        _logger.LogWarning("Frame '{Name}' not found in FrameLookup", name);
        return new Rect();
    }

    public void Dispose()
    {
        Texture?.Dispose();
        Texture = null;
        _spriteSheet = null;
    }
}
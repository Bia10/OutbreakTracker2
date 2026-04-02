using System.Text.Json;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas.Models;

namespace OutbreakTracker2.Application.Services.Atlas;

public class TextureAtlasService(
    ILogger<TextureAtlasService> logger,
    Func<Stream, SpriteSheet, ITextureAtlas> textureAtlasFactory
) : ITextureAtlasService
{
    private readonly ILogger<TextureAtlasService> _logger = logger;
    private readonly Func<Stream, SpriteSheet, ITextureAtlas> _textureAtlasFactory = textureAtlasFactory;
    private readonly List<(string jsonPath, string imagePath, string name)> _atlasConfigs =
    [
        // TODO: maybe load from appsettings.json?
        ("Assets/uiFramesData.json", "Assets/ui.png", AtlasName.UI),
        ("Assets/itemsFramesData.json", "Assets/items.png", AtlasName.Items),
    ];
    private readonly Dictionary<string, ITextureAtlas> _loadedAtlases = [];

    public ITextureAtlas GetAtlas(string name)
    {
        if (_loadedAtlases.TryGetValue(name, out ITextureAtlas? atlas))
            return atlas;

        _logger.LogError("TextureAtlas '{AtlasName}' has not been loaded", name);
        throw new InvalidOperationException($"TextureAtlas '{name}' has not been loaded.");
    }

    public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases() => _loadedAtlases;

    private async Task LoadAtlasesInternalAsync()
    {
        if (_loadedAtlases.Count is not 0)
        {
            _logger.LogWarning("TextureAtlases already loaded. Skipping re-load");
            return;
        }

        string baseDirectory = AppContext.BaseDirectory;

        try
        {
            foreach ((string jsonPath, string imagePath, string name) in _atlasConfigs)
            {
                string fullJsonPath = Path.Combine(baseDirectory, jsonPath);
                string fullImagePath = Path.Combine(baseDirectory, imagePath);

                _logger.LogInformation(
                    "Loading sprite sheet and image for atlas '{AtlasName}' from: JSON='{JsonPath}', Image='{ImagePath}'",
                    name,
                    fullJsonPath,
                    fullImagePath
                );

                SpriteSheet sheet = await TextureAtlasLoader.LoadFromJsonAsync(fullJsonPath).ConfigureAwait(false);

                Stream imageStream;
                if (File.Exists(fullImagePath))
                    imageStream = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                else
                    throw new FileNotFoundException(
                        $"Image file not found at '{fullImagePath}' for TextureAtlas '{name}'. Ensure '{imagePath}' is copied to output directory."
                    );

                _loadedAtlases.Add(name, _textureAtlasFactory(imageStream, sheet));
                _logger.LogInformation("TextureAtlas '{AtlasName}' loaded successfully", name);
            }
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogCritical(ex, "One or more texture atlas files not found. Application cannot start!");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogCritical(ex, "Error deserializing JSON data for texture atlas. Application cannot start!");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "An unexpected error occurred while loading TextureAtlas. Application cannot start!"
            );
            throw;
        }
    }

    public Task LoadAtlasesAsync() => LoadAtlasesInternalAsync();
}

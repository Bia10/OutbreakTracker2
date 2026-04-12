using System.Text.Json;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Atlas.Models;

namespace OutbreakTracker2.Application.Services.Atlas;

public sealed class TextureAtlasService(
    ILogger<TextureAtlasService> logger,
    Func<Stream, SpriteSheet, ITextureAtlas> textureAtlasFactory,
    TextureAtlasOptions options
) : ITextureAtlasService
{
    private readonly ILogger<TextureAtlasService> _logger = logger;
    private readonly Func<Stream, SpriteSheet, ITextureAtlas> _textureAtlasFactory = textureAtlasFactory;
    private readonly IReadOnlyList<TextureAtlasDefinition> _atlasConfigs = ValidateOptions(options);
    private readonly Dictionary<string, ITextureAtlas> _loadedAtlases = new(StringComparer.OrdinalIgnoreCase);

    public ITextureAtlas GetAtlas(string name)
    {
        if (_loadedAtlases.TryGetValue(name, out ITextureAtlas? atlas))
            return atlas;

        _logger.LogError("TextureAtlas '{AtlasName}' has not been loaded", name);
        throw new InvalidOperationException($"TextureAtlas '{name}' has not been loaded.");
    }

    public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases() => _loadedAtlases;

    private static IReadOnlyList<TextureAtlasDefinition> ValidateOptions(TextureAtlasOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Atlases.Count == 0)
            throw new InvalidOperationException("At least one texture atlas must be configured.");

        List<TextureAtlasDefinition> validatedAtlases = [];
        HashSet<string> seenNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (TextureAtlasDefinition atlas in options.Atlases)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(atlas.Name);
            ArgumentException.ThrowIfNullOrWhiteSpace(atlas.JsonPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(atlas.ImagePath);

            if (!seenNames.Add(atlas.Name))
                throw new InvalidOperationException($"Duplicate texture atlas configuration '{atlas.Name}'.");

            validatedAtlases.Add(atlas);
        }

        return validatedAtlases;
    }

    private async Task LoadAtlasesCore()
    {
        if (_loadedAtlases.Count is not 0)
        {
            _logger.LogWarning("TextureAtlases already loaded. Skipping re-load");
            return;
        }

        string baseDirectory = AppContext.BaseDirectory;

        try
        {
            foreach (TextureAtlasDefinition atlas in _atlasConfigs)
            {
                string fullJsonPath = Path.Combine(baseDirectory, atlas.JsonPath);
                string fullImagePath = Path.Combine(baseDirectory, atlas.ImagePath);

                _logger.LogInformation(
                    "Loading sprite sheet and image for atlas '{AtlasName}' from: JSON='{JsonPath}', Image='{ImagePath}'",
                    atlas.Name,
                    fullJsonPath,
                    fullImagePath
                );

                SpriteSheet sheet = await TextureAtlasLoader.LoadFromJsonAsync(fullJsonPath).ConfigureAwait(false);

                if (!File.Exists(fullImagePath))
                    throw new FileNotFoundException(
                        $"Image file not found at '{fullImagePath}' for TextureAtlas '{atlas.Name}'. Ensure '{atlas.ImagePath}' is copied to output directory."
                    );

                using Stream imageStream = new FileStream(
                    fullImagePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );
                _loadedAtlases.Add(atlas.Name, _textureAtlasFactory(imageStream, sheet));
                _logger.LogInformation("TextureAtlas '{AtlasName}' loaded successfully", atlas.Name);
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

    public Task LoadAtlasesAsync() => LoadAtlasesCore();
}

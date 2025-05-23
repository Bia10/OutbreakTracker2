using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.TextureAtlas.Models;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Character;
using OutbreakTracker2.Outbreak.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.TextureAtlas;

public class TextureAtlasService : ITextureAtlasService
{
    private readonly ILogger<TextureAtlasService> _logger;
    private readonly Func<Stream, SpriteSheet, ITextureAtlas> _textureAtlasFactory;
    private readonly List<(string jsonPath, string imagePath, string name)> _atlasConfigs;
    private readonly Dictionary<string, ITextureAtlas> _loadedAtlases = new();

    public TextureAtlasService(ILogger<TextureAtlasService> logger, Func<Stream, SpriteSheet, ITextureAtlas> textureAtlasFactory)
    {
        _logger = logger;
        _textureAtlasFactory = textureAtlasFactory;
        _atlasConfigs =
        [
            // TODO: maybe load from appsettings.json?
            ("Assets/uiFramesData.json", "Assets/ui.png", "UI"),
            //("Assets/itemsFramesData.json", "Assets/items.png", "Items")
        ];
    }

    public ITextureAtlas GetAtlas(string name)
    {
        if (_loadedAtlases.TryGetValue(name, out ITextureAtlas? atlas))
            return atlas;

        _logger.LogError("TextureAtlas '{AtlasName}' has not been loaded", name);
        throw new InvalidOperationException($"TextureAtlas '{name}' has not been loaded.");
    }

    public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases()
        => _loadedAtlases;

    // Todo: this solely exist so that we don't need async app initialization
    // We async load the atlas in the background and block the UI thread until it's done
    // The blocking operation either succeeds or throws an exception which ultimately ends up with Environment.Exit(1)
    // The are 2 fundamental issues to solve:
    // 1. implement support for async app initialization, for which we don't have much use case yet
    // 2. there is no reason to crash the app init, if we fail to build up the texture atlas just log the error and use dummy fallbacks
    public void LoadAtlases()
    {
        // ReSharper disable once AsyncApostle.AsyncWait
        // ReSharper disable once AsyncApostle.AsyncAwaitMayBeElidedHighlighting
        Task.Run(async () => await LoadAtlasesInternalAsync().ConfigureAwait(false)).Wait();
    }

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
            foreach ((string jsonPath, string imagePath, string name) config in _atlasConfigs)
            {
                string fullJsonPath = Path.Combine(baseDirectory, config.jsonPath);
                string fullImagePath = Path.Combine(baseDirectory, config.imagePath);

                _logger.LogInformation("Loading sprite sheet and image for atlas '{AtlasName}' from: JSON='{JsonPath}', Image='{ImagePath}'",
                    config.name, fullJsonPath, fullImagePath);

                SpriteSheet sheet = await TextureAtlasLoader.LoadFromJsonAsync(fullJsonPath).ConfigureAwait(false);

                Stream imageStream;
                if (File.Exists(fullImagePath))
                    imageStream = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                else
                    throw new FileNotFoundException(
                        $"Image file not found at '{fullImagePath}' for TextureAtlas '{config.name}'. Ensure '{config.imagePath}' is copied to output directory.");

                _loadedAtlases.Add(config.name, _textureAtlasFactory(imageStream, sheet));
                _logger.LogInformation("TextureAtlas '{AtlasName}' loaded successfully", config.name);
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
            _logger.LogCritical(ex, "An unexpected error occurred while loading TextureAtlas. Application cannot start!");
            throw;
        }
    }

    public Task LoadAtlasesAsync()
        => LoadAtlasesInternalAsync();

    public string GetSpriteNameFromCharacterType(CharacterBaseType characterType)
    {
        string spriteName = EnumUtility.GetEnumString(characterType, CharacterBaseType.Unknown);

        if (!spriteName.StartsWith("bust", StringComparison.OrdinalIgnoreCase))
            spriteName = $"bust{spriteName}";

        _logger.LogDebug("Obtained sprite name '{SpriteName}' for character type '{CharacterType}'", spriteName, characterType);
        return spriteName;
    }

    public string GetSpriteNameFromScenarioName(Scenario scenarioName)
    {
        string spriteName = EnumUtility.GetEnumString(scenarioName, Scenario.Unknown);

        _logger.LogDebug("Obtained sprite name '{SpriteName}' for scenario name '{ScenarioName}'", spriteName, scenarioName);
        return spriteName;
    }
}
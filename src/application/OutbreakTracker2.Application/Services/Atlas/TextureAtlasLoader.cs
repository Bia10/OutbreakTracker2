using System.Text.Json;
using OutbreakTracker2.Application.Services.Atlas.Models;
using OutbreakTracker2.Application.Services.Atlas.Serialization;

namespace OutbreakTracker2.Application.Services.Atlas;

public static class TextureAtlasLoader
{
    /// <summary>
    /// Loads SpriteSheet from a JSON file.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the JSON file.</param>
    /// <returns>A new instance of SpriteSheet populated from the JSON data.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified JSON file does not exist.</exception>
    /// <exception cref="JsonException">Thrown if there is an error during JSON deserialization.</exception>
    public static async Task<SpriteSheet> LoadFromJsonAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"The file '{filePath}' was not found.");

        FileStream stream = File.OpenRead(filePath);
        await using (stream.ConfigureAwait(false))
        {
            SpriteSheet? spriteSheet =
                await JsonSerializer
                    .DeserializeAsync(stream, SpriteSheetJsonContext.Default.SpriteSheet)
                    .ConfigureAwait(false)
                ?? throw new JsonException("Failed to deserialize sprite sheet data. Deserialized object was null.");
            if (spriteSheet.Frames is null || spriteSheet.Frames.Count is 0)
                throw new JsonException(
                    "Failed to deserialize sprite sheet data. Deserialized object did not contain any frames."
                );

            spriteSheet.BuildFrameLookup();
            return spriteSheet;
        }
    }
}

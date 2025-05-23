using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.Sandbox;

public class Frame
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class SpriteSheet
{
    [JsonPropertyName("frames")]
    public List<Frame> Frames { get; set; } = [];

    [JsonPropertyName("sheetContentWidth")]
    public int SheetContentWidth { get; set; }

    [JsonPropertyName("sheetContentHeight")]
    public int SheetContentHeight { get; set; }

    [JsonIgnore]
    public Dictionary<string, Frame> FrameLookup { get; private set; } = new();

    /// <summary>
    /// Builds a lookup dictionary for frames by their name.
    /// This should be called after deserialization.
    /// </summary>
    public void BuildFrameLookup()
    {
        FrameLookup = Frames.ToDictionary(f => f.Name, f => f);
        Console.WriteLine($"Built frame lookup for {FrameLookup.Count} entries.");
    }
}

[JsonSerializable(typeof(SpriteSheet))]
[JsonSerializable(typeof(List<Frame>))]
public partial class SpriteSheetJsonContext : JsonSerializerContext
{
}

public class Program
{
    public static async Task Main(string[] args)
    {
        const string jsonContent = """
                                   {
                                               "frames": [
                                                   {
                                                       "name": "below freezing point",
                                                       "x": 830,
                                                       "y": 784,
                                                       "width": 256,
                                                       "height": 192
                                                   },
                                                   {
                                                       "name": "bustAlyssa",
                                                       "x": 0,
                                                       "y": 0,
                                                       "width": 256,
                                                       "height": 256
                                                   },
                                                   {
                                                       "name": "bustCindy",
                                                       "x": 0,
                                                       "y": 256,
                                                       "width": 256,
                                                       "height": 256
                                                   }
                                               ],
                                               "sheetContentWidth": 1280,
                                               "sheetContentHeight": 1280
                                           }
                                   """;

        const string filePath = "spriteSheet.json";
        await File.WriteAllTextAsync(filePath, jsonContent);

        try
        {
            SpriteSheet spriteSheetInfo = await TextureAtlasLoader.LoadFromJsonAsync(filePath);

            Console.WriteLine("\nLoaded SpriteSheet Info:");
            Console.WriteLine($"  Sheet Content Width: {spriteSheetInfo.SheetContentWidth}");
            Console.WriteLine($"  Sheet Content Height: {spriteSheetInfo.SheetContentHeight}");
            Console.WriteLine($"  Number of Frames: {spriteSheetInfo.Frames.Count}");

            if (spriteSheetInfo.FrameLookup.TryGetValue("bustCindy", out Frame? cindyFrame))
            {
                Console.WriteLine($"  Found 'bustCindy': X={cindyFrame.X}, Y={cindyFrame.Y}");
            }
            else
            {
                Console.WriteLine("  'bustCindy' frame not found in lookup.");
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}

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

        await using FileStream stream = File.OpenRead(filePath);

        SpriteSheet? spriteSheet = await JsonSerializer
            .DeserializeAsync(stream, SpriteSheetJsonContext.Default.SpriteSheet)
            .ConfigureAwait(false);

        if (spriteSheet is null)
            throw new JsonException("Failed to deserialize sprite sheet data. Deserialized object was null.");

        if (spriteSheet.Frames is null || spriteSheet.Frames.Count is 0)
            throw new JsonException("Failed to deserialize sprite sheet data. Deserialized object did not contain any frames.");

        spriteSheet.BuildFrameLookup();
        Console.WriteLine($"Successfully deserialized {spriteSheet.Frames.Count} frames.");
        return spriteSheet;
    }
}

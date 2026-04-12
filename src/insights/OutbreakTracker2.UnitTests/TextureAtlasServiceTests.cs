using Avalonia;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application;
using OutbreakTracker2.Application.Services.Atlas;

namespace OutbreakTracker2.UnitTests;

public sealed class TextureAtlasServiceTests
{
    [Test]
    public async Task CompositionRoot_BindsTextureAtlasDefinitions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["TextureAtlases:Atlases:0:Name"] = "ui",
                    ["TextureAtlases:Atlases:0:JsonPath"] = "Assets/uiFramesData.json",
                    ["TextureAtlases:Atlases:0:ImagePath"] = "Assets/ui.png",
                    ["TextureAtlases:Atlases:1:Name"] = "items",
                    ["TextureAtlases:Atlases:1:JsonPath"] = "Assets/itemsFramesData.json",
                    ["TextureAtlases:Atlases:1:ImagePath"] = "Assets/items.png",
                }
            )
            .Build();

        TextureAtlasOptions options = CompositionRoot.GetTextureAtlasOptions(configuration);

        await Assert.That(options.Atlases.Count).IsEqualTo(2);
        await Assert.That(options.Atlases[0].Name).IsEqualTo("ui");
        await Assert.That(options.Atlases[1].Name).IsEqualTo("items");
    }

    [Test]
    public async Task LoadAtlasesAsync_LoadsConfiguredAtlasDefinitions()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"OutbreakTracker2.TextureAtlas.{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            string jsonPath = Path.Combine(tempDirectory, "atlas.json");
            string imagePath = Path.Combine(tempDirectory, "atlas.bin");

            await File.WriteAllTextAsync(
                jsonPath,
                """
                {
                  "frames": [
                    {
                      "name": "test-frame",
                      "x": 0,
                      "y": 0,
                      "width": 16,
                      "height": 16
                    }
                  ],
                  "sheetContentWidth": 16,
                  "sheetContentHeight": 16
                                }
                """
            );
            await File.WriteAllBytesAsync(imagePath, [0x01, 0x02, 0x03]);

            int factoryCallCount = 0;
            int frameCount = 0;
            long imageLength = 0;

            TextureAtlasService service = new(
                NullLogger<TextureAtlasService>.Instance,
                (stream, sheet) =>
                {
                    factoryCallCount++;
                    frameCount = sheet.Frames.Count;
                    imageLength = stream.Length;
                    return new StubTextureAtlas();
                },
                new TextureAtlasOptions
                {
                    Atlases =
                    [
                        new TextureAtlasDefinition
                        {
                            Name = "custom",
                            JsonPath = jsonPath,
                            ImagePath = imagePath,
                        },
                    ],
                }
            );

            await service.LoadAtlasesAsync();

            await Assert.That(factoryCallCount).IsEqualTo(1);
            await Assert.That(frameCount).IsEqualTo(1);
            await Assert.That(imageLength).IsEqualTo(3);
            await Assert.That(service.GetAllAtlases().Count).IsEqualTo(1);
            await Assert.That(service.GetAllAtlases().ContainsKey("custom")).IsTrue();
            await Assert.That(service.GetAtlas("custom")).IsTypeOf<StubTextureAtlas>();
            await Assert.That(ReferenceEquals(service.GetAtlas("custom"), service.GetAtlas("CUSTOM"))).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private sealed class StubTextureAtlas : ITextureAtlas
    {
        public Bitmap? Texture => null;

        public Rect GetSourceRectangle(string name) => default;
    }
}

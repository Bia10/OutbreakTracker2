using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Atlas;

namespace OutbreakTracker2.UnitTests;

public sealed class SpriteNameResolverTests
{
    [Test]
    public async Task GetSpriteNameFromItemName_DoesNotDoublePrefix_FileTwoNames()
    {
        SpriteNameResolver resolver = new(NullLogger<SpriteNameResolver>.Instance);

        string spriteName = resolver.GetSpriteNameFromItemName("File Two/Green Herb");

        await Assert.That(spriteName).IsEqualTo("File Two/Green Herb");
    }

    [Test]
    public async Task GetSpriteNameFromItemName_AddsPrefix_WhenMissing()
    {
        SpriteNameResolver resolver = new(NullLogger<SpriteNameResolver>.Instance);

        string spriteName = resolver.GetSpriteNameFromItemName("Green Herb");

        await Assert.That(spriteName).IsEqualTo("File Two/Green Herb");
    }
}

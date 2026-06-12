using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.UnitTests;

public class EEmemOffsetResolverTests
{
    [Test]
    public async Task Resolve_WithNoAdditionalOffsets_ReturnsBasePlusPointerOffset()
    {
        nint resolved = EEmemOffsetResolver.Resolve((nint)0x10000000, (nint)0x230000);

        await Assert.That(resolved).IsEqualTo((nint)0x10230000);
    }

    [Test]
    public async Task Resolve_WithMultipleOffsets_AddsAllOffsetsWithoutDereferencing()
    {
        nint resolved = EEmemOffsetResolver.Resolve((nint)0x10000000, (nint)0x628DA0, [(nint)0x4, (nint)0x20]);

        await Assert.That(resolved).IsEqualTo((nint)0x10628DC4);
    }
}

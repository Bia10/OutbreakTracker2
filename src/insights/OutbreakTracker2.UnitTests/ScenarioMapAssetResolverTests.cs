using OutbreakTracker2.Application.Views.Map.Canvas;

namespace OutbreakTracker2.UnitTests;

public sealed class ScenarioMapAssetResolverTests
{
    [Test]
    public async Task GetCandidateRelativePaths_IncludesRoomSpecificAndScenarioDefaultAssets()
    {
        IReadOnlyList<string> candidates = ScenarioMapAssetResolver.GetCandidateRelativePaths(
            "End of the road",
            "Waiting room",
            roomId: 1
        );

        string roomSpecificPath = Path.Combine("Assets", "Maps", "end-of-the-road", "r0060100.png");
        string scenarioDefaultPath = Path.Combine("Assets", "Maps", "end-of-the-road", "default.png");
        string[] candidateArray = [.. candidates];

        await Assert.That(candidates.Contains(roomSpecificPath, StringComparer.Ordinal)).IsTrue();
        await Assert.That(candidates.Contains(scenarioDefaultPath, StringComparer.Ordinal)).IsTrue();
        await Assert
            .That(Array.IndexOf(candidateArray, roomSpecificPath) < Array.IndexOf(candidateArray, scenarioDefaultPath))
            .IsTrue();
    }

    [Test]
    public async Task ResolveRelativePath_PrefersRoomCodeAsset_WhenPresent()
    {
        string? resolvedPath = ScenarioMapAssetResolver.ResolveRelativePath(
            "End of the road",
            "Central passage 1",
            roomId: 2
        );

        await Assert.That(resolvedPath).IsEqualTo(Path.Combine("Assets", "Maps", "end-of-the-road", "r0060200.png"));
    }

    [Test]
    public async Task ResolveRelativePath_FallsBackToScenarioDefault_WhenRoomSpecificAssetIsMissing()
    {
        const string scenarioSlug = "map-test-scenario-default";
        string scenarioDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", scenarioSlug);
        string scenarioDefaultPath = Path.Combine(scenarioDirectory, "default.png");

        Directory.CreateDirectory(scenarioDirectory);
        await File.WriteAllBytesAsync(scenarioDefaultPath, []);

        try
        {
            string? resolvedPath = ScenarioMapAssetResolver.ResolveRelativePath(
                "Map Test Scenario Default",
                "Room That Does Not Exist",
                roomId: -1
            );

            await Assert.That(resolvedPath).IsEqualTo(Path.Combine("Assets", "Maps", scenarioSlug, "default.png"));
        }
        finally
        {
            if (Directory.Exists(scenarioDirectory))
                Directory.Delete(scenarioDirectory, recursive: true);
        }
    }

    [Test]
    public async Task DetailCandidateRelativePaths_UseBucketOrdinalConvention_ForEndOfTheRoad()
    {
        IReadOnlyList<string> candidates = ScenarioMapDetailAssetResolver.GetCandidateRelativePaths(
            "End of the road",
            "reoutbreak2_umbrellaresearchfacility",
            roomOrdinal: 11
        );

        string gifPath = Path.Combine(
            "Assets",
            "Maps",
            "end-of-the-road",
            "details",
            "reoutbreak2_umbrellaresearchfacility",
            "11b.gif"
        );
        string pngFallbackPath = Path.Combine(
            "Assets",
            "Maps",
            "end-of-the-road",
            "details",
            "reoutbreak2_umbrellaresearchfacility",
            "11.png"
        );

        await Assert.That(candidates.Contains(gifPath, StringComparer.Ordinal)).IsTrue();
        await Assert.That(candidates.Contains(pngFallbackPath, StringComparer.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ResolveDetailRelativePath_PrefersBucketOverlayAsset_WhenPresent()
    {
        const string scenarioSlug = "map-test-scenario-details";
        string detailDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Maps",
            scenarioSlug,
            "details",
            "bucket"
        );
        string overlayPath = Path.Combine(detailDirectory, "2b.gif");

        Directory.CreateDirectory(detailDirectory);
        await File.WriteAllBytesAsync(overlayPath, [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]);

        try
        {
            string? resolvedPath = ScenarioMapDetailAssetResolver.ResolveRelativePath(
                "Map Test Scenario Details",
                "bucket",
                roomOrdinal: 2
            );

            await Assert
                .That(resolvedPath)
                .IsEqualTo(Path.Combine("Assets", "Maps", scenarioSlug, "details", "bucket", "2b.gif"));
        }
        finally
        {
            string scenarioDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", scenarioSlug);
            if (Directory.Exists(scenarioDirectory))
                Directory.Delete(scenarioDirectory, recursive: true);
        }
    }

    [Test]
    public async Task TryLoadRoomDetail_DoesNotThrowWithoutRenderInterface_AndLoadsWhenAvailable()
    {
        const string scenarioSlug = "map-test-scenario-detail-load";
        string detailDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Maps",
            scenarioSlug,
            "details",
            "bucket"
        );
        string overlayPath = Path.Combine(detailDirectory, "1b.png");
        MapSectionGeometry section = new(width: 1, height: 1, rooms: [], detailAssetBucket: "bucket");

        Directory.CreateDirectory(detailDirectory);
        await File.WriteAllBytesAsync(
            overlayPath,
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO7Z2ioAAAAASUVORK5CYII="
            )
        );

        try
        {
            bool loaded = ScenarioMapDetailAssetResolver.TryLoadRoomDetail(
                "Map Test Scenario Detail Load",
                section,
                roomOrdinal: 1,
                out ScenarioMapDetailAssetResolver.DetailBitmapData? detailBitmap
            );

            if (loaded)
            {
                await Assert.That(detailBitmap).IsNotNull();
                await Assert.That(detailBitmap!.Width).IsEqualTo(1);
                await Assert.That(detailBitmap.Height).IsEqualTo(1);
                await Assert.That(detailBitmap.Pixels.Length).IsEqualTo(4);
            }
            else
            {
                await Assert.That(detailBitmap).IsNull();
            }
        }
        finally
        {
            string scenarioDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", scenarioSlug);
            if (Directory.Exists(scenarioDirectory))
                Directory.Delete(scenarioDirectory, recursive: true);
        }
    }

    [Test]
    public async Task SectionDetailCandidateRelativePaths_UseLocalCompositeConvention()
    {
        IReadOnlyList<string> candidates = ScenarioMapDetailAssetResolver.GetSectionCandidateRelativePaths(
            "End of the road",
            "reoutbreak2_umbrellaresearchfacility"
        );

        string compositePath = Path.Combine(
            "Assets",
            "Maps",
            "end-of-the-road",
            "details",
            "reoutbreak2_umbrellaresearchfacility",
            "composite.gif"
        );
        string mapPath = Path.Combine(
            "Assets",
            "Maps",
            "end-of-the-road",
            "details",
            "reoutbreak2_umbrellaresearchfacility",
            "map.png"
        );

        await Assert.That(candidates.Contains(compositePath, StringComparer.Ordinal)).IsTrue();
        await Assert.That(candidates.Contains(mapPath, StringComparer.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ResolveSectionDetailRelativePath_PrefersLocalCompositeAsset_WhenPresent()
    {
        const string scenarioSlug = "map-test-scenario-section-details";
        string detailDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Maps",
            scenarioSlug,
            "details",
            "bucket"
        );
        string compositePath = Path.Combine(detailDirectory, "composite.gif");

        Directory.CreateDirectory(detailDirectory);
        await File.WriteAllBytesAsync(compositePath, [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]);

        try
        {
            string? resolvedPath = ScenarioMapDetailAssetResolver.ResolveSectionRelativePath(
                "Map Test Scenario Section Details",
                "bucket"
            );

            await Assert
                .That(resolvedPath)
                .IsEqualTo(Path.Combine("Assets", "Maps", scenarioSlug, "details", "bucket", "composite.gif"));
        }
        finally
        {
            string scenarioDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps", scenarioSlug);
            if (Directory.Exists(scenarioDirectory))
                Directory.Delete(scenarioDirectory, recursive: true);
        }
    }
}

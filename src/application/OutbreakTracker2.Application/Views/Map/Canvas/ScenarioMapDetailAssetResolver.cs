using System.Collections.Concurrent;
using System.Globalization;
using Avalonia;
using Avalonia.Media.Imaging;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal static class ScenarioMapDetailAssetResolver
{
    private static readonly ConcurrentDictionary<string, DetailBitmapData?> DetailCache = new(
        StringComparer.OrdinalIgnoreCase
    );
    private static readonly string[] SupportedExtensions = [".gif", ".png", ".webp", ".jpg", ".jpeg"];

    public static bool TryLoadSectionDetail(
        string scenarioName,
        MapSectionGeometry section,
        out DetailBitmapData? detailBitmap
    )
    {
        detailBitmap = null;
        if (string.IsNullOrWhiteSpace(section.DetailAssetBucket))
            return false;

        string? path = ResolveSectionRelativePath(scenarioName, section.DetailAssetBucket);
        if (string.IsNullOrWhiteSpace(path))
            return false;

        detailBitmap = DetailCache.GetOrAdd(path, static key => LoadBitmapData(key));
        return detailBitmap is not null;
    }

    public static bool TryLoadRoomDetail(
        string scenarioName,
        MapSectionGeometry section,
        int roomOrdinal,
        out DetailBitmapData? detailBitmap
    )
    {
        detailBitmap = null;
        if (roomOrdinal < 1 || string.IsNullOrWhiteSpace(section.DetailAssetBucket))
            return false;

        string? relativePath = ResolveRelativePath(scenarioName, section.DetailAssetBucket, roomOrdinal);
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        detailBitmap = DetailCache.GetOrAdd(relativePath, static path => LoadBitmapData(path));
        return detailBitmap is not null;
    }

    internal static IReadOnlyList<string> GetCandidateRelativePaths(
        string scenarioName,
        string detailAssetBucket,
        int roomOrdinal
    )
    {
        string scenarioSlug = MapAssetNameUtility.GetScenarioSlug(scenarioName);
        if (string.IsNullOrWhiteSpace(scenarioSlug) || string.IsNullOrWhiteSpace(detailAssetBucket) || roomOrdinal < 1)
            return [];

        string roomOrdinalText = roomOrdinal.ToString(CultureInfo.InvariantCulture);
        List<string> candidates = [];

        foreach (string extension in SupportedExtensions)
        {
            candidates.Add(
                Path.Combine(
                    "Assets",
                    "Maps",
                    scenarioSlug,
                    "details",
                    detailAssetBucket,
                    $"{roomOrdinalText}b{extension}"
                )
            );
        }

        foreach (string extension in SupportedExtensions)
        {
            candidates.Add(
                Path.Combine(
                    "Assets",
                    "Maps",
                    scenarioSlug,
                    "details",
                    detailAssetBucket,
                    $"{roomOrdinalText}{extension}"
                )
            );
        }

        return candidates;
    }

    internal static string? ResolveRelativePath(string scenarioName, string detailAssetBucket, int roomOrdinal)
    {
        foreach (string relativePath in GetCandidateRelativePaths(scenarioName, detailAssetBucket, roomOrdinal))
        {
            string absolutePath = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (File.Exists(absolutePath))
                return relativePath;
        }

        return null;
    }

    internal static IReadOnlyList<string> GetSectionCandidateRelativePaths(
        string scenarioName,
        string detailAssetBucket
    )
    {
        string scenarioSlug = MapAssetNameUtility.GetScenarioSlug(scenarioName);
        if (string.IsNullOrWhiteSpace(scenarioSlug) || string.IsNullOrWhiteSpace(detailAssetBucket))
            return [];

        List<string> candidates = [];

        foreach (string extension in SupportedExtensions)
        {
            candidates.Add(
                Path.Combine("Assets", "Maps", scenarioSlug, "details", detailAssetBucket, $"composite{extension}")
            );
        }

        foreach (string extension in SupportedExtensions)
        {
            candidates.Add(
                Path.Combine("Assets", "Maps", scenarioSlug, "details", detailAssetBucket, $"section{extension}")
            );
        }

        foreach (string extension in SupportedExtensions)
        {
            candidates.Add(
                Path.Combine("Assets", "Maps", scenarioSlug, "details", detailAssetBucket, $"map{extension}")
            );
        }

        return candidates;
    }

    internal static string? ResolveSectionRelativePath(string scenarioName, string detailAssetBucket)
    {
        foreach (string relativePath in GetSectionCandidateRelativePaths(scenarioName, detailAssetBucket))
        {
            string absolutePath = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (File.Exists(absolutePath))
                return relativePath;
        }

        return null;
    }

    private static DetailBitmapData? LoadBitmapData(string path)
    {
        string absolutePath = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        if (!File.Exists(absolutePath))
            return null;

        try
        {
            using FileStream stream = File.OpenRead(absolutePath);
            using Bitmap bitmap = new(stream);

            PixelSize pixelSize = bitmap.PixelSize;
            int stride = pixelSize.Width * 4;
            byte[] pixels = new byte[stride * pixelSize.Height];

            unsafe
            {
                fixed (byte* pixelBuffer = pixels)
                {
                    bitmap.CopyPixels(
                        new PixelRect(0, 0, pixelSize.Width, pixelSize.Height),
                        (nint)pixelBuffer,
                        pixels.Length,
                        stride
                    );
                }
            }

            return new DetailBitmapData(pixelSize.Width, pixelSize.Height, pixels);
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("IPlatformRenderInterface", StringComparison.Ordinal))
        {
            return null;
        }
    }

    internal sealed record DetailBitmapData(int Width, int Height, byte[] Pixels);
}

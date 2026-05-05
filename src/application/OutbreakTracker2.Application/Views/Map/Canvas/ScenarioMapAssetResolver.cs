using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal static class ScenarioMapAssetResolver
{
    private static readonly ConcurrentDictionary<string, Bitmap?> BitmapCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".webp"];

    public static string? ResolveRelativePath(string scenarioName, string roomName, short roomId)
    {
        foreach (string relativePath in GetCandidateRelativePaths(scenarioName, roomName, roomId))
        {
            string absolutePath = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (File.Exists(absolutePath))
                return relativePath;
        }

        return null;
    }

    public static Bitmap? LoadBitmap(string scenarioName, string? relativePath, short roomId, string roomName)
    {
        if (
            ScenarioMapGeometryRenderer.TryLoadBitmap(
                scenarioName,
                relativePath,
                roomId,
                roomName,
                out Bitmap? geometryBitmap
            )
        )
            return geometryBitmap;

        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        return LoadRawBitmap(relativePath);
    }

    private static Bitmap? LoadRawBitmap(string relativePath) =>
        BitmapCache.GetOrAdd(
            relativePath,
            static path =>
            {
                string absolutePath = Path.Combine(AppContext.BaseDirectory, path);
                if (!File.Exists(absolutePath))
                    return null;

                using FileStream stream = File.OpenRead(absolutePath);
                return new Bitmap(stream);
            }
        );

    internal static IReadOnlyList<string> GetCandidateRelativePaths(string scenarioName, string roomName, short roomId)
    {
        string scenarioSlug = MapAssetNameUtility.GetScenarioSlug(scenarioName);
        if (string.IsNullOrWhiteSpace(scenarioSlug))
            return [];

        string resolvedRoomName = ResolveRoomName(scenarioName, roomName, roomId);
        string roomSlug = MapAssetNameUtility.Slugify(resolvedRoomName);
        List<string> candidates = [];

        if (MapAssetNameUtility.TryGetRoomAssetBaseName(scenarioName, roomId, out string roomAssetBaseName))
        {
            foreach (string extension in SupportedExtensions)
                candidates.Add(Path.Combine("Assets", "Maps", scenarioSlug, $"{roomAssetBaseName}{extension}"));
        }

        if (!string.IsNullOrWhiteSpace(roomSlug))
        {
            foreach (string extension in SupportedExtensions)
            {
                candidates.Add(Path.Combine("Assets", "Maps", scenarioSlug, $"{roomSlug}{extension}"));
                candidates.Add(Path.Combine("Assets", "Maps", $"{scenarioSlug}-{roomSlug}{extension}"));
            }
        }

        foreach (string extension in SupportedExtensions)
        {
            candidates.Add(Path.Combine("Assets", "Maps", scenarioSlug, $"default{extension}"));
            candidates.Add(Path.Combine("Assets", "Maps", $"{scenarioSlug}{extension}"));
        }

        return candidates;
    }

    private static string ResolveRoomName(string scenarioName, string roomName, short roomId)
    {
        if (!string.IsNullOrWhiteSpace(roomName))
            return roomName;

        if (roomId < 0)
            return string.Empty;

        return EnumUtility.TryParseByValueOrMember(scenarioName, out Scenario scenario)
            ? scenario.GetRoomName(roomId)
            : string.Empty;
    }
}

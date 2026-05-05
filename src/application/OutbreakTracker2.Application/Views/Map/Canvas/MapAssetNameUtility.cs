using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal static class MapAssetNameUtility
{
    public static string GetScenarioSlug(string scenarioName) => Slugify(scenarioName);

    public static string GetCalibrationTargetKey(
        string scenarioName,
        string? relativePath,
        IReadOnlyDictionary<string, string>? calibrationGroups
    )
    {
        string? assetKey = GetAssetCalibrationKey(relativePath);
        if (
            !string.IsNullOrWhiteSpace(assetKey)
            && calibrationGroups is not null
            && calibrationGroups.TryGetValue(assetKey, out string? calibrationGroupKey)
            && !string.IsNullOrWhiteSpace(calibrationGroupKey)
        )
            return calibrationGroupKey;

        if (!string.IsNullOrWhiteSpace(assetKey))
            return assetKey;

        return GetScenarioSlug(scenarioName);
    }

    public static string? GetAssetCalibrationKey(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        string normalizedPath = relativePath.Replace('\\', '/');
        const string prefix = "Assets/Maps/";
        if (!normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        string remainder = normalizedPath[prefix.Length..];
        int separatorIndex = remainder.LastIndexOf('/');
        if (separatorIndex < 0)
            return Path.GetFileNameWithoutExtension(remainder);

        string scenarioSegment = remainder[..separatorIndex];
        string fileName = Path.GetFileNameWithoutExtension(remainder[(separatorIndex + 1)..]);
        return string.IsNullOrWhiteSpace(fileName) ? scenarioSegment : $"{scenarioSegment}/{fileName}";
    }

    public static bool TryGetRoomAssetBaseName(string scenarioName, short roomId, out string baseName)
    {
        if (roomId < 0 || !EnumUtility.TryParseByValueOrMember(scenarioName, out Scenario scenario))
        {
            baseName = string.Empty;
            return false;
        }

        baseName = $"r{(short)scenario:000}{roomId:00}00";
        return true;
    }

    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        Span<char> buffer = stackalloc char[value.Length];
        int length = 0;
        bool previousWasDash = false;

        foreach (char ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[length++] = char.ToLowerInvariant(ch);
                previousWasDash = false;
                continue;
            }

            if (previousWasDash || length == 0)
                continue;

            buffer[length++] = '-';
            previousWasDash = true;
        }

        if (length > 0 && buffer[length - 1] == '-')
            length--;

        return new string(buffer[..length]);
    }
}

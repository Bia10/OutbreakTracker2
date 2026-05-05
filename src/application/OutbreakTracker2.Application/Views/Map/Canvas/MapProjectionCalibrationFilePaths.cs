namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal static class MapProjectionCalibrationFilePaths
{
    private const string AppFolderName = "OutbreakTracker2";
    private const string OverrideFileName = "map-projection-overrides.json";
    private const string ProfilesFileName = "map-projection-profiles.json";

    public static IEnumerable<string> GetDefaultProfilesPaths()
    {
        string mapsFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "Maps");
        if (!Directory.Exists(mapsFolder))
            yield break;

        foreach (string scenarioDir in Directory.EnumerateDirectories(mapsFolder))
            yield return Path.Combine(scenarioDir, ProfilesFileName);
    }

    public static string GetOverrideProfilesPath()
    {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(basePath, AppFolderName, OverrideFileName);
    }
}

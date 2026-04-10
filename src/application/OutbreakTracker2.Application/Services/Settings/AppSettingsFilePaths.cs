namespace OutbreakTracker2.Application.Services.Settings;

internal static class AppSettingsFilePaths
{
    private const string AppFolderName = "OutbreakTracker2";
    private const string UserSettingsFileName = "user-settings.json";

    public static string GetUserSettingsPath()
    {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(basePath, AppFolderName, UserSettingsFileName);
    }
}

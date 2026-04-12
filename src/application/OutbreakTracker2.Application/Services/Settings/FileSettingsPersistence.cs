namespace OutbreakTracker2.Application.Services.Settings;

internal sealed class FileSettingsPersistence : ISettingsPersistence
{
    public FileSettingsPersistence(string userSettingsPath)
    {
        UserSettingsPath = string.IsNullOrWhiteSpace(userSettingsPath)
            ? throw new ArgumentException("User settings path cannot be null or whitespace.", nameof(userSettingsPath))
            : userSettingsPath;
    }

    public string UserSettingsPath { get; }

    public bool Exists() => File.Exists(UserSettingsPath);

    public void EnsureDirectoryExists()
    {
        string? directoryPath = Path.GetDirectoryName(UserSettingsPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);
    }

    public FileStream OpenRead() => new(UserSettingsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    public FileStream OpenWrite() => new(UserSettingsPath, FileMode.Create, FileAccess.Write, FileShare.None);

    public void Delete()
    {
        if (Exists())
            File.Delete(UserSettingsPath);
    }
}

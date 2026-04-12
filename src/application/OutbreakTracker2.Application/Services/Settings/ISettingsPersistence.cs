namespace OutbreakTracker2.Application.Services.Settings;

internal interface ISettingsPersistence
{
    string UserSettingsPath { get; }

    bool Exists();

    void EnsureDirectoryExists();

    FileStream OpenRead();

    FileStream OpenWrite();

    void Delete();
}

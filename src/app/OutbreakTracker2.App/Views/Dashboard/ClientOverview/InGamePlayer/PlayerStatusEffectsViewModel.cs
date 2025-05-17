using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class PlayerStatusEffectsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _bleedTime = string.Empty;

    [ObservableProperty]
    private string _antiVirusTime = string.Empty;

    [ObservableProperty]
    private string _antiVirusGTime = string.Empty;

    [ObservableProperty]
    private string _herbTime = string.Empty;

    public void Update(
        ushort bleedTime,
        ushort antiVirusTime,
        ushort antiVirusGTime,
        ushort herbTime,
        string status,
        byte currentGameFile)
    {
        BleedTime = TimeUtility.FormatBleedTime(bleedTime, status);
        AntiVirusTime = TimeUtility.FormatAntivirusOrHerbTime(antiVirusTime, herbTime);
        AntiVirusGTime = TimeUtility.FormatAntivirusGTime(antiVirusGTime, currentGameFile);
        HerbTime = TimeUtility.FormatAntivirusOrHerbTime(antiVirusTime, herbTime);
    }
}
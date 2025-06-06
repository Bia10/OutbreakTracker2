using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreEffectsVisible))]
    private bool _isBleedActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreEffectsVisible))]
    private bool _isAntiVirusActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreEffectsVisible))]
    private bool _isAntiVirusGActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreEffectsVisible))]
    private bool _isHerbActive;

    public bool AreEffectsVisible => IsBleedActive || IsAntiVirusActive || IsAntiVirusGActive || IsHerbActive;

    public void Update(
        ushort bleedTimeValue,
        ushort antiVirusTimeValue,
        ushort antiVirusGTimeValue,
        ushort herbTimeValue,
        string status,
        byte currentGameFile)
    {
        IsBleedActive = bleedTimeValue > 0;
        IsAntiVirusActive = antiVirusTimeValue > 0;
        IsAntiVirusGActive = antiVirusGTimeValue > 0;
        IsHerbActive = herbTimeValue > 0;

        BleedTime = TimeUtility.FormatBleedTime(bleedTimeValue, status);
        AntiVirusTime = TimeUtility.FormatAntivirusOrHerbTime(antiVirusTimeValue, herbTimeValue);
        AntiVirusGTime = TimeUtility.FormatAntivirusGTime(antiVirusGTimeValue, currentGameFile);
        HerbTime = TimeUtility.FormatAntivirusOrHerbTime(antiVirusTimeValue, herbTimeValue);
    }
}
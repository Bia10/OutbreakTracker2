using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;

public sealed partial class PlayerStatusEffectsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _bleedTime = string.Empty;

    [ObservableProperty]
    private string _antivirusTime = string.Empty;

    [ObservableProperty]
    private string _antivirusGTime = string.Empty;

    [ObservableProperty]
    private string _herbTime = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreEffectsVisible))]
    private bool _isBleedActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreEffectsVisible))]
    private bool _isAntivirusActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreEffectsVisible))]
    private bool _isAntivirusGActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreEffectsVisible))]
    private bool _isHerbActive;

    public bool AreEffectsVisible => IsBleedActive || IsAntivirusActive || IsAntivirusGActive || IsHerbActive;

    public void Update(
        ushort bleedTimeValue,
        ushort antiVirusTimeValue,
        ushort antiVirusGTimeValue,
        ushort herbTimeValue,
        string status,
        byte currentGameFile
    )
    {
        IsBleedActive = bleedTimeValue > 0;
        IsAntivirusActive = antiVirusTimeValue > 0;
        IsAntivirusGActive = antiVirusGTimeValue > 0;
        IsHerbActive = herbTimeValue > 0;

        BleedTime = TimeUtility.FormatBleedTime(bleedTimeValue, status);
        AntivirusTime = TimeUtility.FormatAntivirusTime(antiVirusTimeValue);
        AntivirusGTime = TimeUtility.FormatAntivirusGTime(antiVirusGTimeValue, currentGameFile);
        HerbTime = TimeUtility.FormatHerbTime(herbTimeValue);
    }
}

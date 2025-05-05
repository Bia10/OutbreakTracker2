using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class PlayerStatusEffectsViewModel : ObservableObject
{
    [ObservableProperty]
    private ushort _bleedTime;

    [ObservableProperty]
    private ushort _antiVirusTime;

    [ObservableProperty]
    private ushort _antiVirusGTime;

    [ObservableProperty]
    private ushort _herbTime;

    public void Update(
        ushort bleedTime,
        ushort antiVirusTime,
        ushort antiVirusGTime,
        ushort herbTime)
    {
        BleedTime = bleedTime;
        AntiVirusTime = antiVirusTime;
        AntiVirusGTime = antiVirusGTime;
        HerbTime = herbTime;
    }
}
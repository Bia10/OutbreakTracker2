using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class PlayerGaugesViewModel : ObservableObject
{
    [ObservableProperty]
    private short _currentHealth;

    [ObservableProperty]
    private short _maximumHealth;

    [ObservableProperty]
    private double _healthPercentage;

    [ObservableProperty]
    private int _curVirus;

    [ObservableProperty]
    private int _maxVirus;

    [ObservableProperty]
    private double _virusPercentage;

    public void Update(
        short currentHealth,
        short maximumHealth,
        double healthPercentage,
        int curVirus,
        int maxVirus,
        double virusPercentage)
    {
        CurrentHealth = currentHealth;
        MaximumHealth = maximumHealth;
        HealthPercentage = healthPercentage;
        CurVirus = curVirus;
        MaxVirus = maxVirus;
        VirusPercentage = virusPercentage;
    }
}
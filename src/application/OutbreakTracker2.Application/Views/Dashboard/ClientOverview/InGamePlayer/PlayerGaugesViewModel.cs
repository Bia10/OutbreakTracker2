using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;

public sealed partial class PlayerGaugesViewModel : ObservableObject
{
    private const double MinProgressPercentage = 0.0;
    private const double MaxProgressPercentage = 100.0;

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
        double virusPercentage
    )
    {
        CurrentHealth = currentHealth;
        MaximumHealth = maximumHealth;
        HealthPercentage = ClampProgressPercentage(healthPercentage);
        CurVirus = curVirus;
        MaxVirus = maxVirus;
        VirusPercentage = ClampProgressPercentage(virusPercentage);
    }

    private static double ClampProgressPercentage(double percentage) =>
        Math.Clamp(percentage, MinProgressPercentage, MaxProgressPercentage);
}

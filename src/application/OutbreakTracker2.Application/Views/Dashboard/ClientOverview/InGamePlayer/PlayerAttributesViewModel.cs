using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class PlayerAttributesViewModel : ObservableObject
{
    [ObservableProperty]
    private float _criticalBonus;

    [ObservableProperty]
    private float _size;

    [ObservableProperty]
    private float _power;

    [ObservableProperty]
    private float _speed;

    public void Update(
        float criticalBonus,
        float size,
        float power,
        float speed)
    {
        CriticalBonus = criticalBonus;
        Size = size;
        Power = power;
        Speed = speed;
    }
}
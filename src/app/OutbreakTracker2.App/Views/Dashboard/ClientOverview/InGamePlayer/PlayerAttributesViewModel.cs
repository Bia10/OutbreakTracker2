using CommunityToolkit.Mvvm.ComponentModel;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class PlayerAttributesViewModel : ObservableObject
{
    [ObservableProperty]
    private float _critBonus;

    [ObservableProperty]
    private float _size;

    [ObservableProperty]
    private float _power;

    [ObservableProperty]
    private float _speed;

    public void Update(
        float critBonus,
        float size,
        float power,
        float speed)
    {
        CritBonus = critBonus;
        Size = size;
        Power = power;
        Speed = speed;
    }
}
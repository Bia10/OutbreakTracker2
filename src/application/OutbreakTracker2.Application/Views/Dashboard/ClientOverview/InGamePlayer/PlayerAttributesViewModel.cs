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

    // TODO: critical bonus
    // In Resident Evil Outbreak, Jim Chapman's personal item Coin increases his chances of landing a critical attack anywhere from %15 to %45.
    // In Resident Evil Outbreak File #2, his Lucky Coin can boost it by an additional %5 to anyone who possess it in their inventory.

    // lmao... findings differ greatly JimsCoin gives 10% on heads stacks up to 100% a tail hit resets back to 0%
    // LuckyCoin appears to have no effect?

    // [EnumMember(Value = "Jim's Coin")]
    // JimsCoin = 403,

    // [EnumMember(Value = "Lucky Coin")]
    // LuckyCoin = 419,
}
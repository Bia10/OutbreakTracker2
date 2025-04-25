using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class InGamePlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private DecodedInGamePlayer _Player = null!;

    [ObservableProperty]
    private byte nameId;

    [ObservableProperty]
    private string characterName = null!;

    [ObservableProperty]
    private string characterType = null!;

    [ObservableProperty]
    private short currentHealthPoints;

    [ObservableProperty]
    private short maximumHealthPoints;

    [ObservableProperty]
    private string healthPointsPercentage = null!;

    [ObservableProperty]
    private string condition = null!;

    [ObservableProperty]
    private string status = null!;

    [ObservableProperty]
    private ushort bleedTime;

    [ObservableProperty]
    private ushort antiVirusTime;

    [ObservableProperty]
    private ushort herbTime;

    [ObservableProperty]
    private int curVirus;

    [ObservableProperty]
    private int maxVirus;

    [ObservableProperty]
    private string virusPercentage = null!;

    [ObservableProperty]
    private float critBonus;

    [ObservableProperty]
    private float size;

    [ObservableProperty]
    private float power;

    [ObservableProperty]
    private float speed;

    [ObservableProperty]
    private float positionX;

    [ObservableProperty]
    private float positionY;

    [ObservableProperty]
    private string roomName = null!;

    [ObservableProperty]
    private byte inventory;

    [ObservableProperty]
    private byte specialItem;

    [ObservableProperty]
    private byte specialInventory;

    [ObservableProperty]
    private byte deadInventory;

    [ObservableProperty]
    private byte cindyBag;

    [ObservableProperty]
    private byte equippedItem;

    [ObservableProperty]
    private bool isEnabled;

    [ObservableProperty]
    private bool isInGame;

    public InGamePlayerViewModel(DecodedInGamePlayer player)
    {
        Update(player);
    }

    public void Update(DecodedInGamePlayer player)
    {
        Player = player;
        NameId = player.NameId;
        CharacterName = player.CharacterName;
        CharacterType = player.CharacterType;
        CurrentHealthPoints = player.CurrentHealthPoints;
        MaximumHealthPoints = player.MaximumHealthPoints;
        HealthPointsPercentage = player.HealthPointsPercentage;
        Condition = player.Condition;
        Status = player.Status;
        BleedTime = player.BleedTime;
        AntiVirusTime = player.AntiVirusTime;
        HerbTime = player.HerbTime;
        CurVirus = player.CurVirus;
        MaxVirus = player.MaxVirus;
        VirusPercentage = player.VirusPercentage;
        CritBonus = player.CritBonus;
        Size = player.Size;
        Power = player.Power;
        Speed = player.Speed;
        PositionX = player.PositionX;
        PositionY = player.PositionY;
        RoomName = player.RoomName;
        Inventory = player.Inventory;
        SpecialItem = player.SpecialItem;
        SpecialInventory = player.SpecialInventory;
        DeadInventory = player.DeadInventory;
        CindyBag = player.CindyBag;
        EquippedItem = player.EquippedItem;
        IsEnabled = player.Enabled;
        IsInGame = player.InGame;
    }
}
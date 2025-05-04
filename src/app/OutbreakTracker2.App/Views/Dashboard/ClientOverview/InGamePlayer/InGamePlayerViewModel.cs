using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class InGamePlayerViewModel : ObservableObject
{

    [ObservableProperty]
    private byte nameId;

    [ObservableProperty]
    private string characterName = string.Empty;

    [ObservableProperty]
    private string characterType = string.Empty;

    [ObservableProperty]
    private short currentHealth;

    [ObservableProperty]
    private short maximumHealth;

    [ObservableProperty]
    private double healthPercentage;

    [ObservableProperty]
    private string conditionTitle = "Condition:";

    [ObservableProperty]
    private string conditionMessage = string.Empty;

    [ObservableProperty]
    private NotificationType conditionSeverity = NotificationType.Information;

    [ObservableProperty]
    private bool isConditionVisible = false;

    [ObservableProperty]
    private string statusTitle = "Status:";

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private NotificationType statusSeverity = NotificationType.Information;

    [ObservableProperty]
    private bool isStatusVisible = false;

    [ObservableProperty]
    private ushort bleedTime;

    [ObservableProperty]
    private ushort antiVirusTime;

    [ObservableProperty]
    private ushort antiVirusGTime;

    [ObservableProperty]
    private ushort herbTime;

    [ObservableProperty]
    private int curVirus;

    [ObservableProperty]
    private int maxVirus;

    [ObservableProperty]
    private double virusPercentage;

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
    private string roomName = string.Empty;

    [ObservableProperty]
    private byte equippedItem;

    [ObservableProperty]
    private bool isEnabled;

    [ObservableProperty]
    private bool isInGame;

    [ObservableProperty]
    private InventoryViewModel _inventory;

    private readonly IDataManager _dataManager;
    private DecodedInGamePlayer _playerModel = null!;
    
    public InGamePlayerViewModel(DecodedInGamePlayer player, IDataManager dataManager)
    {
        _dataManager = dataManager;
        _inventory = new InventoryViewModel(dataManager);
        
        Update(player);
    }

    /// <summary>
    /// Updates the ViewModel's properties based on the latest player data model.
    /// </summary>
    public void Update(DecodedInGamePlayer player)
    {
        _playerModel = player;
        NameId = player.NameId;
        CharacterName = player.CharacterName;
        CharacterType = player.CharacterType;
        CurrentHealth = player.CurrentHealth;
        MaximumHealth = player.MaximumHealth;
        HealthPercentage = player.HealthPercentage;
        BleedTime = player.BleedTime;
        AntiVirusTime = player.AntiVirusTime;
        AntiVirusGTime = player.AntiVirusGTime;
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
        EquippedItem = player.EquippedItem;
        IsEnabled = player.Enabled;
        IsInGame = player.InGame;
        
        string rawCondition = player.Condition;
        IsConditionVisible = !string.IsNullOrEmpty(rawCondition);
        if (IsConditionVisible)
        {
            ConditionTitle = "Condition:";
            ConditionMessage = rawCondition;
            ConditionSeverity = ConvertCondition(rawCondition);
        }

        string rawStatus = player.Status;
        IsStatusVisible = !string.IsNullOrEmpty(rawStatus);
        if (IsStatusVisible)
        {
            StatusTitle = "Status:";
            StatusMessage = rawStatus;
            StatusSeverity = ConvertStatus(rawStatus);
        }
        
        RoomName = UpdateRoomName(player);
        
        UpdateInventory(player);
    }

    public string UpdateRoomName(DecodedInGamePlayer player)
    {
        string curScenarioName = _dataManager.InGameScenario.ScenarioName;
        if (!string.IsNullOrEmpty(curScenarioName) && EnumUtility.TryParseByValueOrMember(curScenarioName, out InGameScenario scenarioEnum))
            player.RoomName = scenarioEnum.GetRoomName(player.RoomId);

        return player.RoomName;
    }

    private void UpdateInventory(DecodedInGamePlayer player)
    {
        Inventory.UpdateFromPlayerData(
            player.EquippedItem,
            player.Inventory,
            player.SpecialItem,
            player.SpecialInventory,
            player.DeadInventory,
            player.SpecialDeadInventory
        );
    }
    
    private static NotificationType ConvertCondition(string value)
    {
        return value.ToLower() switch
        {
            "fine" => NotificationType.Success,
            "caution2" => NotificationType.Warning,
            "caution" => NotificationType.Warning,
            "gas" => NotificationType.Warning,
            "danger" => NotificationType.Error,
            "down" => NotificationType.Error,
            "down+gas" => NotificationType.Error,
            "" => NotificationType.Error,
            _ => NotificationType.Error 
        };
    }

    private static NotificationType ConvertStatus(string value)
    {
        return value switch
        {
            "OK" => NotificationType.Success,
            "Dead" => NotificationType.Error,
            "Zombie" => NotificationType.Error,
            "Down" => NotificationType.Warning,
            "Gas" => NotificationType.Warning,
            "Bleed" => NotificationType.Warning,
            "" => NotificationType.Error, 
            _ => NotificationType.Error 
        };
    }
}
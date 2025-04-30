using System;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using SukiUI.Controls;

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
    private short currentHealth;

    [ObservableProperty]
    private short maximumHealth;

    [ObservableProperty]
    private double healthPercentage;

    [ObservableProperty]
    private string condition = null!;

    [ObservableProperty]
    private string status = null!;

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
    private string roomName = null!;

    [ObservableProperty]
    private byte _inventorySlot1;

    [ObservableProperty]
    private byte _inventorySlot2;

    [ObservableProperty]
    private byte _inventorySlot3;

    [ObservableProperty]
    private byte _inventorySlot4;

    [ObservableProperty]
    private byte specialItem;

    [ObservableProperty]
    private byte specialInventorySlot1;

    [ObservableProperty]
    private byte specialInventorySlot2;

    [ObservableProperty]
    private byte specialInventorySlot3;

    [ObservableProperty]
    private byte specialInventorySlot4;

    [ObservableProperty]
    private byte deadInventorySlot1;
    
    [ObservableProperty]
    private byte deadInventorySlot2;

    [ObservableProperty]
    private byte deadInventorySlot3;
    
    [ObservableProperty]
    private byte deadInventorySlot4;
    
    [ObservableProperty]
    private byte specialDeadInventorySlot1;
    
    [ObservableProperty]
    private byte specialDeadInventorySlot2;

    [ObservableProperty]
    private byte specialDeadInventorySlot3;
    
    [ObservableProperty]
    private byte specialDeadInventorySlot4;
    
    [ObservableProperty]
    private byte equippedItem;

    [ObservableProperty]
    private bool isEnabled;

    [ObservableProperty]
    private bool isInGame;

    [ObservableProperty]
    private InfoBar _conditionBadge;

    [ObservableProperty]
    private InfoBar _statusBadge;

    public InGamePlayerViewModel(DecodedInGamePlayer player)
    {
        _conditionBadge = CreateInfoBar("Condition:", "");
        _statusBadge = CreateInfoBar("Status:", "");

        Update(player);
    }

    public void Update(DecodedInGamePlayer player)
    {
        Player = player;
        NameId = player.NameId;
        CharacterName = player.CharacterName;
        CharacterType = player.CharacterType;
        CurrentHealth = player.CurrentHealth;
        MaximumHealth = player.MaximumHealth;
        HealthPercentage = player.HealthPercentage;
        Condition = player.Condition;
        Status = player.Status;
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
        RoomName = player.RoomName;
        InventorySlot1 = player.Inventory[0];
        InventorySlot2 = player.Inventory[1];
        InventorySlot3 = player.Inventory[2];
        InventorySlot4 = player.Inventory[3];
        SpecialItem = player.SpecialItem;
        SpecialInventorySlot1 = player.SpecialInventory[0];
        SpecialInventorySlot2 = player.SpecialInventory[1];
        SpecialInventorySlot3 = player.SpecialInventory[2];
        SpecialInventorySlot4 = player.SpecialInventory[3];
        DeadInventorySlot1 = player.DeadInventory[0];
        DeadInventorySlot2 = player.DeadInventory[1];
        DeadInventorySlot3 = player.DeadInventory[2];
        DeadInventorySlot4 = player.DeadInventory[3];
        SpecialDeadInventorySlot1 = player.SpecialDeadInventory[0];
        SpecialDeadInventorySlot2 = player.SpecialDeadInventory[1];
        SpecialDeadInventorySlot3 = player.SpecialDeadInventory[2];
        SpecialDeadInventorySlot4 = player.SpecialDeadInventory[3];
        EquippedItem = player.EquippedItem;
        IsEnabled = player.Enabled;
        IsInGame = player.InGame;

        UpdateBadge(ConditionBadge,
            player.Condition,
            ConvertCondition(player.Condition),
            "Condition:");

        UpdateBadge(StatusBadge,
            player.Status,
            ConvertStatus(player.Status),
            "Status:");
    }

    private static InfoBar CreateInfoBar(string title, string initialMessage)
        => new()
        {
            Title = title,
            IsOpen = true,
            IsClosable = false,
            Message = initialMessage,
            Severity = NotificationType.Information
        };

    private static void UpdateBadge(InfoBar badge, string message, NotificationType severity, string title)
    {
        badge.Message = message;
        badge.Severity = severity;
        badge.Title = title;
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
            _ => throw new InvalidOperationException("Current condition type is not recognized:  " + value)
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
            _ => throw new InvalidOperationException("Current status type is not recognized:  " + value)
        };
    }
}
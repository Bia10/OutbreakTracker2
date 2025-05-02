using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Inventory;
using OutbreakTracker2.Outbreak.Models;
using SukiUI.Controls;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;

public partial class InGamePlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private DecodedInGamePlayer _player = null!;

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
    private string condition = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

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
    private InfoBar _conditionBadge;

    [ObservableProperty]
    private InfoBar _statusBadge;

    [ObservableProperty]
    private InventoryViewModel _inventory;

    public InGamePlayerViewModel(DecodedInGamePlayer player, IDataManager dataManager)
    {
        _conditionBadge = CreateInfoBar("Condition:", string.Empty);
        _statusBadge = CreateInfoBar("Status:", string.Empty);

        Player = player;
        _inventory = new InventoryViewModel(dataManager);

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

        UpdateInventory(player);
    }

    public void UpdateInventory(DecodedInGamePlayer player)
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
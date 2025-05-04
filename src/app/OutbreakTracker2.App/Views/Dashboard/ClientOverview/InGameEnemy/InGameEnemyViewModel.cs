using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using SukiUI.Controls;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemy;

public partial class InGameEnemyViewModel : ObservableObject
{
    [ObservableProperty]
    private DecodedEnemy _enemy = null!;

    [ObservableProperty]
    private short slotId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private short currentHp;

    [ObservableProperty]
    private short maxHp;

    [ObservableProperty]
    private double healthPercentage;

    [ObservableProperty]
    private string bossType = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private string roomName = string.Empty;

    [ObservableProperty]
    private InfoBar _statusBadge;

    private readonly IDataManager _dataManager;

    public InGameEnemyViewModel(DecodedEnemy enemy, IDataManager dataManager)
    {
        _dataManager = dataManager;
        _statusBadge = CreateInfoBar("Status:", string.Empty);
        Update(enemy);
    }

    public void Update(DecodedEnemy enemy)
    {
        Enemy = enemy;
        SlotId = enemy.SlotId;
        Name = $"{enemy.Name}({enemy.SlotId})";
        CurrentHp = enemy.CurHp;
        MaxHp = enemy.MaxHp;
        HealthPercentage = PercentageUtility.GetPercentage(enemy.CurHp, enemy.MaxHp);
        BossType = ConvertBossType(enemy.BossType);
        Status = ConvertStatus(enemy.Status);
        RoomName = enemy.RoomName;

        UpdateBadge(
            StatusBadge,
            Status,
            GetStatusSeverity(enemy.Status),
            "Status:"
        );

        UpdateRoomName(enemy);
    }

    private void UpdateRoomName(DecodedEnemy enemy)
    {
        string curScenarioName = _dataManager.InGameScenario.ScenarioName;
        if (!string.IsNullOrEmpty(curScenarioName) && EnumUtility.TryParseByValueOrMember(curScenarioName, out InGameScenario scenarioEnum))
            enemy.RoomName = scenarioEnum.GetRoomName(enemy.RoomId);
    }

    private static InfoBar CreateInfoBar(string title, string message) => new()
    {
        Title = title,
        IsOpen = true,
        IsClosable = false,
        Message = message,
        Severity = NotificationType.Information
    };

    private static void UpdateBadge(InfoBar badge, string message, NotificationType severity, string title)
    {
        badge.Message = message;
        badge.Severity = severity;
        badge.Title = title;
    }

    private static string ConvertBossType(byte type) => type switch
    {
        0 => "Normal Enemy",
        1 => "Mini-Boss",
        2 => "Main Boss",
        _ => "Unknown"
    };

    private static string ConvertStatus(byte status) => status switch
    {
        0 => "Inactive",
        1 => "Active",
        2 => "Alerted",
        3 => "Dead",
        _ => "Unknown"
    };

    private static NotificationType GetStatusSeverity(byte status) => status switch
    {
        0 => NotificationType.Information,
        1 => NotificationType.Success,
        2 => NotificationType.Warning,
        3 => NotificationType.Error,
        _ => NotificationType.Information
    };
}
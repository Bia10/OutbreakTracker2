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
    private string _name = string.Empty;

    [ObservableProperty]
    private short _currentHp;

    [ObservableProperty]
    private short _maxHp;

    [ObservableProperty]
    private double _healthPercentage;

    [ObservableProperty]
    private string _bossType = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _roomName = string.Empty;

    public string UniqueId { get; }

    private readonly IDataManager _dataManager;

    public InGameEnemyViewModel(DecodedEnemy enemy, IDataManager dataManager)
    {
        _dataManager = dataManager;
        UniqueId = enemy.Id;
        Update(enemy);
    }

    public void Update(DecodedEnemy enemy)
    {
        if (UniqueId != enemy.Id)
            return;

        Enemy = enemy;
        Name = $"{enemy.Name}({enemy.SlotId})";
        CurrentHp = enemy.CurHp;
        MaxHp = enemy.MaxHp;
        HealthPercentage = PercentageUtility.GetPercentage(enemy.CurHp, enemy.MaxHp);
        BossType = ConvertBossType(enemy.BossType);
        Status = ConvertStatus(enemy.Status);


        UpdateRoomName(enemy);
    }

    private void UpdateRoomName(DecodedEnemy enemy)
    {
        string curScenarioName = _dataManager.InGameScenario.ScenarioName;
        if (!string.IsNullOrEmpty(curScenarioName) && EnumUtility.TryParseByValueOrMember(curScenarioName, out Scenario scenarioEnum))
            RoomName = scenarioEnum.GetRoomName(enemy.RoomId);
        else
            RoomName = $"Room {enemy.RoomId}";
    }


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
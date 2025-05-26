using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Enemy;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemy;

public partial class InGameEnemyViewModel : ObservableObject
{
    [ObservableProperty]
    private DecodedEnemy _enemy = null!;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ushort _currentHp;

    [ObservableProperty]
    private ushort _maxHp;

    [ObservableProperty]
    private double _healthPercentage;

    [ObservableProperty]
    private string _healthStatus = string.Empty;

    [ObservableProperty]
    private string _bossType = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _roomName = string.Empty;

    [ObservableProperty]
    private Color _rawBorderColor;

    public IBrush BorderBrush => new SolidColorBrush(RawBorderColor);

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
        HealthStatus = GetEnemiesHealthStatusStringForFileTwo(enemy.SlotId, enemy.NameId, enemy.CurHp, enemy.MaxHp);
        BossType = ConvertBossType(enemy.BossType);
        Status = ConvertStatus(enemy.Status);
        RoomName = UpdateRoomName(enemy.RoomId);
        RawBorderColor = GetEnemyColorForFileTwo(enemy.SlotId, enemy.NameId);

        OnPropertyChanged(nameof(BorderBrush));
    }

    private string UpdateRoomName(byte enemyRoomId)
    {
        string curScenarioName = _dataManager.InGameScenario.ScenarioName;
        if (!string.IsNullOrEmpty(curScenarioName) && EnumUtility.TryParseByValueOrMember(curScenarioName, out Scenario scenarioEnum))
            return scenarioEnum.GetRoomName(enemyRoomId);

        return $"Room {enemyRoomId}";
    }

    private static string ConvertBossType(byte type) => type switch
    {
        0 => "Normal Enemy",
        1 => "Mini-Boss",
        2 => "Main Boss",
        _ => "Unknown"
    };

    //TODO: a bit weird 0-active, 1-dead
    private static string ConvertStatus(byte status) => status switch
    {
        0 => "Inactive",
        1 => "Active",
        2 => "Alerted",
        3 => "Dead",
        _ => "Unknown"
    };

    public static string GetEnemyHealthStatusStringForFileOne(int slotId, byte nameId, ushort curHp, ushort maxHp)
    {
        if (slotId is < 0 or >= GameConstants.MaxEnemies1)
            return $"Invalid enemy SlotId({slotId})";

        bool enemyTypeParsed = EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType);
        if (!enemyTypeParsed)
            return $"Failed to parse enemyType for nameId {nameId}";

        string healthString = $"{curHp}/{maxHp}";

        if (curHp == 0x7fff || enemyType is EnemyType.Drainer11
                            || enemyType is EnemyType.Drainer12
                            || enemyType is EnemyType.Drainer14
                            || enemyType is EnemyType.Neptune
                            || enemyType is EnemyType.Tentacles
                            || enemyType is EnemyType.LeechTentacles)
            return "Invincible";

        return curHp switch
        {
            0x0 or 0xffff or >= 0x8000 when enemyType is not (EnemyType.Mine or EnemyType.GasolineTank) => "Dead",
            0xffff when maxHp is 0x1 && enemyType is EnemyType.Mine => "Destroyed",
            0x0 when enemyType is EnemyType.GasolineTank => "Exploded",
            _ => healthString
        };
    }

    public static string GetEnemiesHealthStatusStringForFileTwo(int slotId, byte nameId, ushort curHp, ushort maxHp)
    {
        if (slotId is < 0 or >= GameConstants.MaxEnemies2)
            return $"Invalid enemy SlotId({slotId})";

        bool enemyTypeParsed = EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType);
        if (!enemyTypeParsed)
            return $"Failed to parse enemyType for nameId {nameId}";

        string healthString = $"{curHp}/{maxHp}";

        if (curHp is 0x7fff || enemyType is EnemyType.Drainer11
                            || enemyType is EnemyType.Drainer12
                            || enemyType is EnemyType.Drainer14
                            || enemyType is EnemyType.Neptune
                            || enemyType is EnemyType.Tentacles
                            || enemyType is EnemyType.LeechTentacles)
            return "Invincible";

        return curHp switch
        {
            0x0 or 0xffff or >= 0x8000 when enemyType is not (EnemyType.Mine or EnemyType.GasolineTank) => "Dead",
            0xffff when maxHp is 0x1 && enemyType is EnemyType.Mine => "Destroyed",
            0x0 when enemyType is EnemyType.GasolineTank => "Exploded",
            _ => healthString
        };
    }

    public static Color GetEnemyColorForFileOne(int slotId, byte nameId)
    {
        if (slotId is < 0 or >= GameConstants.MaxEnemies1)
            return Color.FromArgb(255, 0, 0, 0);

        bool enemyTypeParsed = EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType);
        if (!enemyTypeParsed)
            return Color.FromArgb(255, 255, 0, 0);

        return enemyType switch
        {
            EnemyType.Mine or EnemyType.GasolineTank or EnemyType.Fire => Color.FromArgb(255, 255, 80, 40),
            EnemyType.Mouse or EnemyType.Rafflesia or EnemyType.Typewriter => Color.FromArgb(255, 0, 255, 0),
            _ => Color.FromArgb(255, 255, 255, 255) 
        };
    }

    public static Color GetEnemyColorForFileTwo(int slotId, byte nameId)
    {
        // if (slotId is < 0 or >= GameConstants.MaxEnemies2)
        // return Color.FromArgb(255, 0, 0, 0);

        bool enemyTypeParsed = EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType);
        if (!enemyTypeParsed)
            return Color.FromArgb(255, 255, 0, 0);

        return enemyType switch
        {
            EnemyType.Mine or EnemyType.GasolineTank or EnemyType.Fire => Color.FromArgb(255, 255, 80, 40),
            EnemyType.Mouse or EnemyType.Rafflesia or EnemyType.Typewriter => Color.FromArgb(255, 0, 255, 0),
            _ => Color.FromArgb(255, 255, 255, 255)
        };
    }

    // TODO: GetBossHealthStatusString and GetBossColor
}
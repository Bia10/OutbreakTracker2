using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Views.Common;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Enemy;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;

public sealed partial class InGameEnemyViewModel : ObservableObject
{
    private static readonly Color InvalidEnemyColor = Color.FromArgb(255, 255, 0, 0);
    private static readonly Color ExplosiveEnemyColor = Color.FromArgb(255, 255, 80, 40);
    private static readonly Color UtilityEnemyColor = Color.FromArgb(255, 0, 255, 0);
    private static readonly Color DefaultEnemyColor = Color.FromArgb(255, 255, 255, 255);
    private static readonly Color InvalidSlotColor = Color.FromArgb(255, 0, 0, 0);

    private static readonly IBrush InvalidEnemyBrush = new ImmutableSolidColorBrush(InvalidEnemyColor);
    private static readonly IBrush ExplosiveEnemyBrush = new ImmutableSolidColorBrush(ExplosiveEnemyColor);
    private static readonly IBrush UtilityEnemyBrush = new ImmutableSolidColorBrush(UtilityEnemyColor);
    private static readonly IBrush DefaultEnemyBrush = new ImmutableSolidColorBrush(DefaultEnemyColor);
    private static readonly IBrush InvalidSlotBrush = new ImmutableSolidColorBrush(InvalidSlotColor);

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
    private bool _isDead;

    [ObservableProperty]
    private bool _isInvincible;

    [ObservableProperty]
    private string _bossType = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private byte _roomId;

    [ObservableProperty]
    private string _roomName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BorderBrush))]
    private Color _rawBorderColor;

    public IBrush BorderBrush => GetBorderBrush(RawBorderColor);

    public Ulid UniqueId { get; }

    /// <summary>Raised when enemy HP decreases, carrying Red as the glow color.</summary>
    public event EventHandler<GlowEventArgs>? GlowTriggered;

    private ushort _previousHp;
    private bool _isFirstUpdate = true;

    public InGameEnemyViewModel(DecodedEnemy enemy, string scenarioName)
    {
        UniqueId = enemy.Id;
        Update(enemy, scenarioName);
    }

    public void Update(DecodedEnemy enemy, string scenarioName)
    {
        if (UniqueId != enemy.Id)
            return;

        if (!_isFirstUpdate && enemy.CurHp < _previousHp)
            GlowTriggered?.Invoke(this, new GlowEventArgs(Colors.Red));

        _previousHp = enemy.CurHp;
        _isFirstUpdate = false;

        Name = $"{enemy.Name}({enemy.SlotId})";
        CurrentHp = enemy.CurHp;
        MaxHp = enemy.MaxHp;
        HealthStatus = EnemyStatusUtility.GetHealthStatusForFileTwo(
            enemy.SlotId,
            enemy.NameId,
            enemy.CurHp,
            enemy.MaxHp,
            enemy.Name
        );
        IsDead = EnemyStatusUtility.IsDeadStatus(HealthStatus);
        IsInvincible = HealthStatus is "Invincible";
        HealthPercentage = IsDead || enemy.MaxHp == 0 ? 0.0 : PercentageUtility.GetPercentage(enemy.CurHp, enemy.MaxHp);
        BossType = ConvertBossType(enemy.BossType);
        Status = ConvertStatus(enemy.Status);
        RoomId = enemy.RoomId;
        RoomName = UpdateRoomName(enemy.RoomId, scenarioName);
        RawBorderColor = GetEnemyColorForFileTwo(enemy.SlotId, enemy.NameId);
    }

    private static string UpdateRoomName(byte enemyRoomId, string scenarioName)
    {
        if (
            !string.IsNullOrEmpty(scenarioName)
            && EnumUtility.TryParseByValueOrMember(scenarioName, out Scenario scenarioEnum)
        )
            return scenarioEnum.GetRoomName(enemyRoomId);

        return $"Room {enemyRoomId}";
    }

    private static IBrush GetBorderBrush(Color color)
    {
        if (color == ExplosiveEnemyColor)
            return ExplosiveEnemyBrush;

        if (color == UtilityEnemyColor)
            return UtilityEnemyBrush;

        if (color == InvalidEnemyColor)
            return InvalidEnemyBrush;

        if (color == InvalidSlotColor)
            return InvalidSlotBrush;

        return DefaultEnemyBrush;
    }

    private static string ConvertBossType(byte type) =>
        type switch
        {
            0 => "Normal Enemy",
            1 => "Mini-Boss",
            2 => "Main Boss",
            _ => "Unknown",
        };

    //TODO: a bit weird 0-active, 1-dead
    private static string ConvertStatus(byte status) =>
        status switch
        {
            0 => "Inactive",
            1 => "Active",
            2 => "Alerted",
            3 => "Dead",
            _ => "Unknown",
        };

    public static Color GetEnemyColorForFileOne(int slotId, byte nameId)
    {
        if (slotId is < 0 or >= GameConstants.MaxEnemies1)
            return InvalidSlotColor;

        bool enemyTypeParsed = EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType);
        if (!enemyTypeParsed)
            return InvalidEnemyColor;

        return enemyType switch
        {
            EnemyType.Mine or EnemyType.GasolineTank or EnemyType.Fire => ExplosiveEnemyColor,
            EnemyType.Mouse or EnemyType.Rafflesia or EnemyType.Typewriter => UtilityEnemyColor,
            _ => DefaultEnemyColor,
        };
    }

    public static Color GetEnemyColorForFileTwo(int slotId, byte nameId)
    {
        if (slotId <= 0)
            return InvalidSlotColor;

        bool enemyTypeParsed = EnumUtility.TryParseByValueOrMember(nameId, out EnemyType enemyType);
        if (!enemyTypeParsed)
            return InvalidEnemyColor;

        return enemyType switch
        {
            EnemyType.Mine or EnemyType.GasolineTank or EnemyType.Fire => ExplosiveEnemyColor,
            EnemyType.Mouse or EnemyType.Rafflesia or EnemyType.Typewriter => UtilityEnemyColor,
            _ => DefaultEnemyColor,
        };
    }

    // TODO: GetBossHealthStatusString and GetBossColor
}

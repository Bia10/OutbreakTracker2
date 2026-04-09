using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoor;

public sealed partial class InGameDoorViewModel : ObservableObject
{
    private static readonly Color BlueBorderColor = Color.FromArgb(255, 0, 100, 255);
    private static readonly Color WhiteBorderColor = Color.FromArgb(255, 255, 255, 255);

    private static readonly IBrush LockedBrush = new ImmutableSolidColorBrush(Colors.Red);
    private static readonly IBrush UnlockedBrush = new ImmutableSolidColorBrush(Colors.LimeGreen);
    private static readonly IBrush DestroyedBrush = new ImmutableSolidColorBrush(Color.FromArgb(255, 180, 60, 60));
    private static readonly IBrush BlueBorderBrush = new ImmutableSolidColorBrush(BlueBorderColor);
    private static readonly IBrush WhiteBorderBrush = new ImmutableSolidColorBrush(WhiteBorderColor);

    [ObservableProperty]
    private ushort _hp;

    [ObservableProperty]
    private ushort _maxHp;

    [ObservableProperty]
    private double _healthPercentage;

    [ObservableProperty]
    private string _hpStatus = string.Empty;

    [ObservableProperty]
    private ushort _flag;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LockForeground))]
    private bool _isLocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DestroyedForeground))]
    private bool _isDestroyed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BorderBrush))]
    private Color _calculatedBorderColor;

    public IBrush BorderBrush => CalculatedBorderColor == BlueBorderColor ? BlueBorderBrush : WhiteBorderBrush;
    public IBrush LockForeground => IsLocked ? LockedBrush : UnlockedBrush;
    public IBrush DestroyedForeground => IsDestroyed ? DestroyedBrush : UnlockedBrush;

    public Ulid UniqueId { get; private set; }

    /// <summary>Raised when a door property changes, carrying the glow color for border animation.</summary>
    public event EventHandler<GlowEventArgs>? GlowTriggered;

    private ushort _previousHp;
    private ushort _previousFlag;
    private string _previousStatus = string.Empty;
    private bool _isFirstUpdate = true;

    public InGameDoorViewModel(DecodedDoor doorData)
    {
        UniqueId = doorData.Id;
        Update(doorData);
    }

    public void Update(DecodedDoor doorData)
    {
        if (UniqueId != doorData.Id)
            return;

        if (!_isFirstUpdate)
        {
            Color? glowColor = DetermineGlowColor(doorData.Hp, doorData.Flag, doorData.Status);
            if (glowColor.HasValue)
                GlowTriggered?.Invoke(this, new GlowEventArgs(glowColor.Value));
        }

        _previousHp = doorData.Hp;
        _previousFlag = doorData.Flag;
        _previousStatus = doorData.Status;
        _isFirstUpdate = false;

        Hp = doorData.Hp;
        if (MaxHp == 0 || doorData.Hp > MaxHp)
            MaxHp = doorData.Hp;
        IsDestroyed = doorData.Hp == 0;
        HealthPercentage = IsDestroyed || MaxHp == 0 ? 0.0 : Math.Clamp(doorData.Hp * 100.0 / MaxHp, 0.0, 100.0);
        HpStatus = IsDestroyed ? "Destroyed" : $"{doorData.Hp}/{MaxHp}";
        Flag = doorData.Flag;
        Status = doorData.Status;
        IsLocked = string.Equals(doorData.Status, "locked", StringComparison.OrdinalIgnoreCase);
        CalculatedBorderColor = GetBorderColor();
    }

    private Color? DetermineGlowColor(ushort newHp, ushort newFlag, string newStatus)
    {
        bool wasLocked = string.Equals(_previousStatus, "locked", StringComparison.OrdinalIgnoreCase);
        bool nowLocked = string.Equals(newStatus, "locked", StringComparison.OrdinalIgnoreCase);
        if (wasLocked != nowLocked)
            return Colors.DodgerBlue;

        if (newFlag != _previousFlag)
            return Colors.Orange;

        if (newHp < _previousHp)
            return Colors.Red;

        if (newHp > _previousHp)
            return Colors.LimeGreen;

        return null;
    }

    private Color GetBorderColor()
    {
        return DoorConstants.IsPassable(Hp, Flag) ? BlueBorderColor : WhiteBorderColor;
    }
}

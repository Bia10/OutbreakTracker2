using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoor;

public partial class InGameDoorViewModel : ObservableObject
{
    private static readonly IBrush LockedBrush = new SolidColorBrush(Colors.Red);
    private static readonly IBrush UnlockedBrush = new SolidColorBrush(Colors.LimeGreen);

    [ObservableProperty]
    private ushort _hp;

    [ObservableProperty]
    private ushort _flag;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LockForeground))]
    private bool _isLocked;

    [ObservableProperty]
    private Color _calculatedBorderColor;

    public IBrush BorderBrush => new SolidColorBrush(CalculatedBorderColor);
    public IBrush LockForeground => IsLocked ? LockedBrush : UnlockedBrush;

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
        Flag = doorData.Flag;
        Status = doorData.Status;
        IsLocked = string.Equals(doorData.Status, "locked", StringComparison.OrdinalIgnoreCase);
        CalculatedBorderColor = GetBorderColor();

        OnPropertyChanged(nameof(BorderBrush));
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
        bool isBlueBorder =
            Hp is 500 || Hp is 0 || Flag is 0x00 || Flag is 0x0A || Flag is 0x0C || Flag is 0x2C || Flag is 0x82;

        return isBlueBorder ? Color.FromArgb(255, 0, 100, 255) : Color.FromArgb(0, 0, 0, 0);
    }
}

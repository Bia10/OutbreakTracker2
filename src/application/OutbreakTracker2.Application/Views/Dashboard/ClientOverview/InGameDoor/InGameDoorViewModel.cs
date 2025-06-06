using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
using System;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoor;

public partial class InGameDoorViewModel : ObservableObject
{
    [ObservableProperty]
    private ushort _hp;

    [ObservableProperty]
    private ushort _flag;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private Color _calculatedBorderColor;

    public IBrush BorderBrush => new SolidColorBrush(CalculatedBorderColor);

    public Ulid UniqueId { get; private set; }

    public InGameDoorViewModel(DecodedDoor doorData)
    {
        UniqueId = doorData.Id;
        Update(doorData);
    }

    public void Update(DecodedDoor doorData)
    {
        if (UniqueId != doorData.Id)
            return;

        Hp = doorData.Hp;
        Flag = doorData.Flag;
        Status = doorData.Status;
        CalculatedBorderColor = GetBorderColor();

        OnPropertyChanged(nameof(BorderBrush));
    }

    private Color GetBorderColor()
    {
        bool isBlueBorder = Hp is 500 || Hp is 0 || Flag is 0x00 || Flag is 0x0A || Flag is 0x0C || Flag is 0x2C || Flag is 0x82;

        return isBlueBorder ? Color.FromArgb(255, 0, 100, 255) : Color.FromArgb(0, 0, 0, 0);
    }
}

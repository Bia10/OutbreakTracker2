using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameDoor;

public partial class InGameDoorViewModel : ObservableObject
{
    [ObservableProperty]
    private ushort _hp;

    [ObservableProperty]
    private ushort _flag;

    [ObservableProperty]
    private string _status = string.Empty;

    public string UniqueId { get; private set; }

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

        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        bool isBlueBorder = Hp == 500 || Hp == 0 || Flag == 0x00 || Flag == 0x0A || Flag == 0x0C || Flag == 0x2C || Flag == 0x82;
        // TODO:
        if (isBlueBorder)
        {
        }
        else
        {
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OutbreakTracker2.App.Services.Data;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.Debug;

public partial class DebugViewModel : ObservableObject
{
    private readonly IDataManager _dataManager;

    public DebugViewModel(IDataManager dataManager)
    {
        _dataManager = dataManager;
    }

    [RelayCommand]
    public void UpdateDoor()
        => _dataManager.UpdateDoors();

    [RelayCommand]
    public void UpdateEnemies()
        => _dataManager.UpdateEnemies();

    [RelayCommand]
    public void UpdateInGamePlayer()
        => _dataManager.UpdateInGamePlayer();

    [RelayCommand]
    public void UpdateInGameScenario()
        => _dataManager.UpdateInGameScenario();

    [RelayCommand]
    public void UpdateLobbyRoom()
        => _dataManager.UpdateLobbyRoom();

    [RelayCommand]
    public void UpdateLobbyRoomPlayers()
        => _dataManager.UpdateLobbyRoomPlayers();

    [RelayCommand]
    public void UpdateLobbySlots()
        => _dataManager.UpdateLobbySlots();

    [RelayCommand]
    public async Task UpdateAll()
        => await Task.CompletedTask;
}
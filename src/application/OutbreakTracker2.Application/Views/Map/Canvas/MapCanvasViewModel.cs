using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

public partial class MapCanvasViewModel(IDataManager dataManager) : ObservableObject
{
    public Observable<DecodedInGamePlayer[]> PlayersObservable { get; } = dataManager.InGamePlayersObservable;

    [ObservableProperty]
    private double _mapWidth = 800;

    [ObservableProperty]
    private double _mapHeight = 600;
}

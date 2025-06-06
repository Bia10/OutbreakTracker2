using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

public partial class MapCanvasViewModel : ObservableObject
{
    public Observable<DecodedInGamePlayer[]> PlayersObservable { get; }

    [ObservableProperty]
    private double _mapWidth = 800;

    [ObservableProperty]
    private double _mapHeight = 600;

    public MapCanvasViewModel(IDataManager dataManager)
    {
        PlayersObservable = dataManager.InGamePlayersObservable;
    }
}
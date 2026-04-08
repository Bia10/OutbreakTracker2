using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

public sealed partial class MapCanvasViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _playersIsInGameSubscription;

    public Observable<DecodedInGamePlayer[]> PlayersObservable { get; }
    public Observable<DecodedEnemy[]> EnemiesObservable { get; }

    [ObservableProperty]
    private bool _isInGame;

    [ObservableProperty]
    private double _mapWidth = 800;

    [ObservableProperty]
    private double _mapHeight = 600;

    public MapCanvasViewModel(IDataManager dataManager, IDispatcherService dispatcherService)
    {
        PlayersObservable = dataManager.InGamePlayersObservable;
        EnemiesObservable = dataManager.EnemiesObservable;

        _playersIsInGameSubscription = dataManager
            .InGamePlayersObservable.ObserveOnThreadPool()
            .Subscribe(players =>
            {
                bool anyInGame = false;
                foreach (DecodedInGamePlayer p in players)
                {
                    if (p.IsEnabled && p.IsInGame)
                    {
                        anyInGame = true;
                        break;
                    }
                }

                dispatcherService.PostOnUI(() => IsInGame = anyInGame);
            });
    }

    public void Dispose() => _playersIsInGameSubscription.Dispose();
}

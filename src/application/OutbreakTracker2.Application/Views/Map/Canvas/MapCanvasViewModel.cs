using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

public sealed partial class MapCanvasViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _playersIsInGameSubscription;
    private readonly ILogger<MapCanvasViewModel> _logger;

    public Observable<DecodedInGamePlayer[]> PlayersObservable { get; }
    public Observable<DecodedEnemy[]> EnemiesObservable { get; }

    [ObservableProperty]
    private bool _isInGame;

    [ObservableProperty]
    private double _mapWidth = 800;

    [ObservableProperty]
    private double _mapHeight = 600;

    public MapCanvasViewModel(
        IDataObservableSource dataObservable,
        IDispatcherService dispatcherService,
        ILogger<MapCanvasViewModel> logger
    )
    {
        _logger = logger;
        PlayersObservable = dataObservable.InGamePlayersObservable;
        EnemiesObservable = dataObservable.EnemiesObservable;

        _playersIsInGameSubscription = dataObservable
            .InGamePlayersObservable.ObserveOnThreadPool()
            .Subscribe(
                onNext: players =>
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
                },
                onErrorResume: ex => _logger.LogError(ex, "Error while monitoring map canvas player state"),
                onCompleted: _ => _logger.LogInformation("Map canvas player-state stream completed")
            );
    }

    public void Dispose() => _playersIsInGameSubscription.Dispose();
}

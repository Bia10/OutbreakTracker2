using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

public sealed partial class MapCanvasViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<MapCanvasViewModel> _logger;
    private DisposableBag _disposables;
    private bool _hasActivePlayers;
    private ScenarioStatus _scenarioStatus;
    private bool _showGameplayUiDuringTransitions;

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
        IAppSettingsService settingsService,
        ILogger<MapCanvasViewModel> logger
    )
    {
        _logger = logger;
        _showGameplayUiDuringTransitions = GetDisplaySettings(settingsService.Current).ShowGameplayUiDuringTransitions;
        PlayersObservable = dataObservable.InGamePlayersObservable;
        EnemiesObservable = dataObservable.EnemiesObservable;

        _disposables.Add(
            dataObservable
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

                        dispatcherService.PostOnUI(() => UpdatePlayerPresence(anyInGame));
                    },
                    onErrorResume: ex => _logger.LogError(ex, "Error while monitoring map canvas player state"),
                    onCompleted: _ => _logger.LogInformation("Map canvas player-state stream completed")
                )
        );

        _disposables.Add(
            dataObservable
                .InGameScenarioObservable.Select(static scenario => scenario.Status)
                .DistinctUntilChanged()
                .Subscribe(
                    onNext: status => dispatcherService.PostOnUI(() => UpdateScenarioStatus(status)),
                    onErrorResume: ex => _logger.LogError(ex, "Error while monitoring map canvas scenario state"),
                    onCompleted: _ => _logger.LogInformation("Map canvas scenario-state stream completed")
                )
        );

        _disposables.Add(
            settingsService
                .SettingsObservable.Select(static settings =>
                    GetDisplaySettings(settings).ShowGameplayUiDuringTransitions
                )
                .DistinctUntilChanged()
                .Subscribe(
                    onNext: show => dispatcherService.PostOnUI(() => UpdateTransitionDisplaySetting(show)),
                    onErrorResume: ex => _logger.LogError(ex, "Error while monitoring map canvas settings"),
                    onCompleted: _ => _logger.LogInformation("Map canvas settings stream completed")
                )
        );
    }

    public void Dispose() => _disposables.Dispose();

    private void UpdatePlayerPresence(bool hasActivePlayers)
    {
        _hasActivePlayers = hasActivePlayers;
        UpdateVisibleState();
    }

    private void UpdateScenarioStatus(ScenarioStatus scenarioStatus)
    {
        _scenarioStatus = scenarioStatus;
        UpdateVisibleState();
    }

    private void UpdateTransitionDisplaySetting(bool showGameplayUiDuringTransitions)
    {
        _showGameplayUiDuringTransitions = showGameplayUiDuringTransitions;
        UpdateVisibleState();
    }

    private void UpdateVisibleState() =>
        IsInGame = _hasActivePlayers && _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions);

    private static DisplaySettings GetDisplaySettings(OutbreakTrackerSettings settings) =>
        settings.Display ?? new DisplaySettings();
}

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Views.GameDock;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;

public sealed partial class InGameScenarioViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<InGameScenarioViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly ScenarioEntityCommands _entityCommands;
    private readonly ScenarioViewModelRouter _router;
    private DisposableBag _disposables;

    public ICommand ShowItemsCommand => _entityCommands.ShowItems;
    public ICommand ShowEnemiesCommand => _entityCommands.ShowEnemies;
    public ICommand ShowDoorsCommand => _entityCommands.ShowDoors;
    public ICommand ShowMapCommand => _entityCommands.ShowMap;

    [ObservableProperty]
    private byte _currentFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScenarioActive))]
    [NotifyPropertyChangedFor(nameof(IsScenarioNotActive))]
    private bool _showGameplayUiDuringTransitions;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    [ObservableProperty]
    private int _frameCounter;

    [ObservableProperty]
    private string _gameTimeDisplay = string.Empty;

    // 0 = scenario not in progress
    // 1 = loading after intro cinematic sequence
    // 2 = in game
    // 3 = loading screen (between room transitions)
    // 4 = cinematic sequence playing
    // 7 = generic loading
    // 8 = unknown
    // 9 = unknown
    // 10 = unknown
    // 11 = unknown
    // 12 = scenario finished (failed/success)
    // 13 = after game stats / scenario clear rank
    // 14 = intro scenario cinematic sequence
    // 15 = after save to memory card
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScenarioActive))]
    [NotifyPropertyChangedFor(nameof(IsScenarioNotActive))]
    private ScenarioStatus _status;

    [ObservableProperty]
    private string _isCleared = string.Empty;

    [ObservableProperty]
    private string _difficulty = string.Empty;

    [ObservableProperty]
    private byte _playerCount;

    [ObservableProperty]
    private string _playerCountDisplay = string.Empty;

    [ObservableProperty]
    private byte _itemRandom;

    [ObservableProperty]
    private byte _itemRandom2;

    [ObservableProperty]
    private byte _puzzleRandom;

    // generic random seed
    [ObservableProperty]
    private byte _gasRandom;

    [ObservableProperty]
    private int _gasRandomOrderDisplay;

    [ObservableProperty]
    private short _escapeTime;

    [ObservableProperty]
    private byte _pass1;

    [ObservableProperty]
    private byte _pass2;

    [ObservableProperty]
    private byte _pass3;

    [ObservableProperty]
    private short _passUnderbelly1;

    [ObservableProperty]
    private byte _passUnderbelly2;

    [ObservableProperty]
    private byte _passUnderbelly3;

    [ObservableProperty]
    private short _pass4;

    [ObservableProperty]
    private byte _pass5;

    [ObservableProperty]
    private byte _pass6;

    [ObservableProperty]
    private ObservableObject? _currentScenarioSpecificViewModel;

    public bool IsScenarioActive => Status.ShouldShowGameplayUi(ShowGameplayUiDuringTransitions);
    public bool IsScenarioNotActive => !IsScenarioActive;

    public InGameScenarioViewModel(
        ILogger<InGameScenarioViewModel> logger,
        IDataObservableSource dataObservable,
        IDispatcherService dispatcherService,
        IAppSettingsService settingsService,
        ScenarioEntityCommands entityCommands,
        ScenarioViewModelRouter router
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _entityCommands = entityCommands;
        _router = router;
        ShowGameplayUiDuringTransitions = GetDisplaySettings(settingsService.Current).ShowGameplayUiDuringTransitions;

        _disposables.Add(
            dataObservable
                .InGameOverviewObservable.ObserveOnThreadPool()
                .SubscribeAwait(
                    async (snapshot, cancellationToken) =>
                    {
                        _logger.LogTrace("Processing in-game overview snapshot on thread pool");
                        try
                        {
                            await dispatcherService
                                .InvokeOnUIAsync(
                                    () =>
                                    {
                                        _logger.LogTrace("Updating InGameScenarioViewModel on UI thread");
                                        Update(snapshot.Scenario, snapshot.Players);
                                    },
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("In-game overview snapshot processing cancelled");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during in-game overview snapshot processing cycle");
                        }
                    },
                    AwaitOperation.Drop
                )
        );

        _disposables.Add(
            settingsService
                .SettingsObservable.Select(static settings =>
                    GetDisplaySettings(settings).ShowGameplayUiDuringTransitions
                )
                .DistinctUntilChanged()
                .Subscribe(show => _dispatcherService.PostOnUI(() => ShowGameplayUiDuringTransitions = show))
        );
    }

    public void Dispose() => _disposables.Dispose();

    public void Update(DecodedInGameScenario scenario, DecodedInGamePlayer[] players)
    {
        CurrentFile = scenario.CurrentFile;
        ScenarioName = scenario.ScenarioName;
        FrameCounter = scenario.FrameCounter;
        Difficulty = scenario.Difficulty;
        Status = scenario.Status;
        PlayerCount = GetTrackedPlayerCount(players);
        PlayerCountDisplay = $"{PlayerCount}/{GameConstants.MaxPlayers}";
        ItemRandom = scenario.ItemRandom;
        ItemRandom2 = scenario.ItemRandom2;
        PuzzleRandom = scenario.PuzzleRandom;
        GasRandom = scenario.GasRandom;
        EscapeTime = scenario.EscapeTime;
        Pass1 = scenario.Pass1;
        Pass2 = scenario.Pass2;
        Pass3 = scenario.Pass3;
        PassUnderbelly1 = scenario.PassUnderbelly1;
        PassUnderbelly2 = scenario.PassUnderbelly2;
        PassUnderbelly3 = scenario.PassUnderbelly3;
        Pass4 = scenario.Pass4;
        Pass5 = scenario.Pass5;
        Pass6 = scenario.Pass6;

        GameTimeDisplay = scenario.GetGameTimeDisplay();
        GasRandomOrderDisplay = scenario.GetGasRandomOrderDisplay();
        IsCleared = GetClearedDisplay();

        UpdateScenarioSpecificViewModel(scenario);
    }

    private static byte GetTrackedPlayerCount(DecodedInGamePlayer[] players)
    {
        byte count = 0;

        foreach (DecodedInGamePlayer player in players)
        {
            if (!player.IsEnabled)
                continue;

            if (player.NameId > 0 || !string.IsNullOrEmpty(player.Type))
                count++;
        }

        return count;
    }

    private void UpdateScenarioSpecificViewModel(DecodedInGameScenario scenario)
    {
        CurrentScenarioSpecificViewModel = _router.Route(scenario, _logger);
    }

    private string GetClearedDisplay() =>
        Status is ScenarioStatus.GameFinished or ScenarioStatus.RankScreen or ScenarioStatus.AfterSaveToMemoryCard
            ? "Yes"
            : "No";

    private static DisplaySettings GetDisplaySettings(OutbreakTrackerSettings settings) =>
        settings.Display ?? new DisplaySettings();
}

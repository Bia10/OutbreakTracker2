using CommunityToolkit.Mvvm.ComponentModel;
using FastEnumUtility;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario.FileOne;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario.FileTwo;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario;

// TODO: needs a rewrite
public partial class InGameScenarioViewModel : ObservableObject
{
    private readonly ILogger<InGameScenarioViewModel> _logger;
    private readonly DesperateTimesViewModel _desperateTimesVm;
    private readonly EndOfTheRoadViewModel _endOfTheRoadVm;
    private readonly UnderbellyViewModel _underbellyVm;
    private readonly WildThingsViewModel _wildThingsVm;
    private readonly HellfireViewModel _hellfireVm;
    private readonly TheHiveViewModel _theHiveVm;
    private readonly DecisionsDecisionsViewModel _decisionsDecisionsVm;
    private readonly BelowFreezingPointViewModel _belowFreezingPointVm;

    [ObservableProperty]
    private byte _currentFile;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    [ObservableProperty]
    private int _frameCounter;

    [ObservableProperty]
    private string _gameTimeDisplay = string.Empty;

    // TODO: unknown statuses
    // 0 = scenario not in progress
    // 1 = loading after cinematic sequence
    // 2 = ingame scenario in progress
    // 7 = loading scenario
    // 12 = finished (died)
    // 13 = after game stats
    // 14 = intro scenario cinematic sequence
    // 15 = after save to memory card
    [ObservableProperty]
    private byte _status;

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

    public InGameScenarioViewModel(
        ILogger<InGameScenarioViewModel> logger,
        IDataManager dataManager,
        IDispatcherService dispatcherService)
    {
        _logger = logger;
        _desperateTimesVm = new DesperateTimesViewModel();
        _endOfTheRoadVm = new EndOfTheRoadViewModel();
        _underbellyVm = new UnderbellyViewModel();
        _wildThingsVm = new WildThingsViewModel();
        _hellfireVm = new HellfireViewModel();
        _theHiveVm = new TheHiveViewModel();
        _decisionsDecisionsVm = new DecisionsDecisionsViewModel();
        _belowFreezingPointVm = new BelowFreezingPointViewModel();

        dataManager.InGameScenarioObservable
            .ObserveOnThreadPool()
            .SubscribeAwait(async (inGameScenario, cancellationToken) =>
            {
                _logger.LogTrace("Processing inGame scenario data on thread pool");
                try
                {
                    await dispatcherService.InvokeOnUIAsync(() =>
                    {
                        _logger.LogTrace("Updating InGameScenarioViewModel properties on UI thread");
                        Update(inGameScenario);
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("InGame scenario data processing cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during inGame scenario data processing cycle");
                }
            }, AwaitOperation.Drop);
    }

    public void Update(DecodedInGameScenario scenario)
    {
        if (scenario.CurrentFile is < 1 or > 2)
            return;

        CurrentFile = scenario.CurrentFile;
        ScenarioName = scenario.ScenarioName;
        FrameCounter = scenario.FrameCounter;
        Difficulty = scenario.Difficulty;
        Status = scenario.Status;
        PlayerCount = scenario.PlayerCount;
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

        GameTimeDisplay = GetGameTime();
        GasRandomOrderDisplay = CalculateGasRandomOrderDisplay();
        IsCleared = GetClearedDisplay();

        UpdateScenarioSpecificViewModel(scenario);
    }

    private void UpdateScenarioSpecificViewModel(DecodedInGameScenario scenario)
    {
        if (string.IsNullOrEmpty(scenario.ScenarioName))
            return;

        CurrentScenarioSpecificViewModel = null;

        switch (FastEnum.Parse<Scenario>(scenario.ScenarioName))
        {
            case Scenario.DesperateTimes:
                _desperateTimesVm.Update(scenario);
                CurrentScenarioSpecificViewModel = _desperateTimesVm;
                break;
            case Scenario.EndOfTheRoad:
                _endOfTheRoadVm.Update(scenario);
                CurrentScenarioSpecificViewModel = _endOfTheRoadVm;
                break;
            case Scenario.Underbelly:
                _underbellyVm.Update(scenario);
                CurrentScenarioSpecificViewModel = _underbellyVm;
                break;
            case Scenario.WildThings:
                _wildThingsVm.Update(scenario);
                CurrentScenarioSpecificViewModel = _wildThingsVm;
                break;
            case Scenario.Hellfire:
                _hellfireVm.Update(scenario);
                CurrentScenarioSpecificViewModel = _hellfireVm;
                break;
            case Scenario.TheHive:
                _theHiveVm.Update(scenario);
                CurrentScenarioSpecificViewModel = _theHiveVm;
                break;
            case Scenario.DecisionsDecisions:
                _decisionsDecisionsVm.Update(scenario);
                CurrentScenarioSpecificViewModel = _decisionsDecisionsVm;
                break;
            case Scenario.BelowFreezingPoint:
                _belowFreezingPointVm.Update(scenario);
                CurrentScenarioSpecificViewModel = _belowFreezingPointVm;
                break;
            // unused for now
            case Scenario.Unknown:
            case Scenario.Outbreak:
            case Scenario.TrainingGround:
            case Scenario.Showdown1:
            case Scenario.Showdown2:
            case Scenario.Showdown3:
            case Scenario.Flashback:
            case Scenario.Elimination3:
            case Scenario.Elimination1:
            case Scenario.Elimination2:

            default:
                _logger.LogInformation("No specific view configured for scenario: {ScenarioName}", scenario.ScenarioName);
                break;
        }
    }

    private string GetClearedDisplay() => Status is 12 or 13 or 15 ? "Yes" : "No";
    private string GetGameTime() => TimeUtility.GetTimeFromFrames(FrameCounter);

    private int CalculateGasRandomOrderDisplay()
    {
        switch (GasRandom)
        {
            case > 0 and < 240: return (GasRandom / 10) + 1;
            case >= 240 and < 255: return 25;
            default:
                _logger.LogDebug("Unrecognized GasRandom value: {GasRandom}", GasRandom);
                return -1;
        }
    }
}
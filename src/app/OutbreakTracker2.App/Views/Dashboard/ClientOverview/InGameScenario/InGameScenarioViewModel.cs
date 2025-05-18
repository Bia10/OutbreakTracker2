using CommunityToolkit.Mvvm.ComponentModel;
using FastEnumUtility;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
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

    // Used in Underbelly, Flashback
    [ObservableProperty]
    private byte _itemRandom;

    // Used in Underbelly, Flashback
    [ObservableProperty]
    private byte _itemRandom2;

    // Used in Underbelly, Flashback, Hellfire
    [ObservableProperty]
    private byte _puzzleRandom;

    // Used in Desperate Times, however this seems more like generic random seed then scenario specific
    // used in calculations unrelated to Desperate Times
    [ObservableProperty]
    private byte _gasRandom;

    [ObservableProperty]
    private int _gasRandomOrderDisplay;

    // Used in "Underbelly" and "Flashback" (Existing properties)
    [ObservableProperty]
    private short _escapeTime;

    // Used for Below Freezing Point and Hive (Existing properties)
    [ObservableProperty]
    private byte _pass1;

    // Used for Below Freezing Point (Existing properties)
    [ObservableProperty]
    private byte _pass2;

    // Used in Hellfire and Decisions,decisions (with pass6) (Existing properties)
    [ObservableProperty]
    private byte _pass3;

    [ObservableProperty]
    private short _passUnderbelly1;

    [ObservableProperty]
    private byte _passUnderbelly2;

    [ObservableProperty]
    private byte _passUnderbelly3;

    // Used for Hellfire, also displayed raw in "End of the Road" (Existing properties)
    [ObservableProperty]
    private short _pass4;

    // Use unclear ?? (Existing properties)
    [ObservableProperty]
    private byte _pass5;

    // Used for Decisions,decisions (with pass3) (Existing properties)
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
            // TODO:
            case Scenario.Hellfire:
            case Scenario.TheHive:
            case Scenario.DecisionsDecisions:
            case Scenario.BelowFreezingPoint:
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
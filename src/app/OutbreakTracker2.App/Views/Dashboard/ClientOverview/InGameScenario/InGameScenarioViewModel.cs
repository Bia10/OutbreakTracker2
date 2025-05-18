using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Collections.Generic;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameScenario;

public partial class InGameScenarioViewModel : ObservableObject
{
    private readonly ILogger<InGameScenarioViewModel> _logger;
    private readonly Dictionary<Scenario, Action<DecodedInGameScenario>> _scenarioUpdateActions;

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
        DesperateTimesViewModel desperateTimesVm = new();
        EndOfTheRoadViewModel endOfTheRoadVm = new();
        UnderbellyViewModel underbellyVm = new();
        WildThingsViewModel wildThingsVm = new();
        HellfireViewModel hellfireVm = new();
        TheHiveViewModel theHiveVm = new();
        DecisionsDecisionsViewModel decisionsDecisionsVm = new();
        BelowFreezingPointViewModel belowFreezingPointVm = new();

        _scenarioUpdateActions = new Dictionary<Scenario, Action<DecodedInGameScenario>>
        {
            { Scenario.DesperateTimes, scenario => { desperateTimesVm.Update(scenario); CurrentScenarioSpecificViewModel = desperateTimesVm; } },
            { Scenario.EndOfTheRoad, scenario => { endOfTheRoadVm.Update(scenario); CurrentScenarioSpecificViewModel = endOfTheRoadVm; } },
            { Scenario.Underbelly, scenario => { underbellyVm.Update(scenario); CurrentScenarioSpecificViewModel = underbellyVm; } },
            { Scenario.WildThings, scenario => { wildThingsVm.Update(scenario); CurrentScenarioSpecificViewModel = wildThingsVm; } },
            { Scenario.Hellfire, scenario => { hellfireVm.Update(scenario); CurrentScenarioSpecificViewModel = hellfireVm; } },
            { Scenario.TheHive, scenario => { theHiveVm.Update(scenario); CurrentScenarioSpecificViewModel = theHiveVm; } },
            { Scenario.DecisionsDecisions, scenario => { decisionsDecisionsVm.Update(scenario); CurrentScenarioSpecificViewModel = decisionsDecisionsVm; } },
            { Scenario.BelowFreezingPoint, scenario => { belowFreezingPointVm.Update(scenario); CurrentScenarioSpecificViewModel = belowFreezingPointVm; } }
        };

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
        if (string.IsNullOrEmpty(scenario.ScenarioName) || scenario.ScenarioName.Equals("Unknown(0)", StringComparison.Ordinal))
            return;

        CurrentScenarioSpecificViewModel = null;

        bool parsedScenario = EnumUtility.TryParseByValueOrMember(scenario.ScenarioName, out Scenario scenarioType);
        if (!parsedScenario)
        {
            _logger.LogWarning("Failed to parse scenario name: {ScenarioName}", scenario.ScenarioName);
            return;
        }

        if (_scenarioUpdateActions.TryGetValue(scenarioType, out Action<DecodedInGameScenario>? updateAction))
            updateAction(scenario);
        else
            _logger.LogInformation("No specific view configured for scenario: {ScenarioName}", scenario.ScenarioName);
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
﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileOne;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileTwo;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;
using SukiUI.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;

public partial class InGameScenarioViewModel : ObservableObject
{
    private readonly ILogger<InGameScenarioViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly Dictionary<Scenario, Action<DecodedInGameScenario>> _scenarioUpdateActions;

    private ISukiDialogManager DialogManager { get; }

    [ObservableProperty]
    private byte _currentFile;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScenarioActive))]
    [NotifyPropertyChangedFor(nameof(IsScenarioNotActive))]
    private int _frameCounter;

    [ObservableProperty]
    private string _gameTimeDisplay = string.Empty;

    // TODO: unknown statuses
    // 0 = scenario not in progress
    // 1 = loading after intro cinematic sequence
    // 2 = scenario in progress
    // 3 = scenario in progress - doors/scene loading
    // 4 = scenario in progress - cinematic sequence playing
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

    [ObservableProperty]
    private ScenarioEntitiesViewModel _scenarioEntitiesVm;

    public bool IsScenarioActive => FrameCounter > 0;
    public bool IsScenarioNotActive => FrameCounter <= 0;

    public InGameScenarioViewModel(
        ILogger<InGameScenarioViewModel> logger,
        IDataManager dataManager,
        ISukiDialogManager dialogManager,
        IDispatcherService dispatcherService)
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        DialogManager = dialogManager;

        DesperateTimesViewModel desperateTimesVm = new();
        EndOfTheRoadViewModel endOfTheRoadVm = new();
        UnderbellyViewModel underbellyVm = new();
        WildThingsViewModel wildThingsVm = new();
        HellfireViewModel hellfireVm = new();
        TheHiveViewModel theHiveVm = new();
        DecisionsDecisionsViewModel decisionsDecisionsVm = new();
        BelowFreezingPointViewModel belowFreezingPointVm = new();

        _scenarioEntitiesVm = new ScenarioEntitiesViewModel();
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

        dataManager.EnemiesObservable.ObserveOnThreadPool()
            .SubscribeAwait(async (enemies, cancellationToken) =>
            {
                _logger.LogTrace("Processing enemies data on thread pool");
                try
                {
                    await dispatcherService.InvokeOnUIAsync(() =>
                    {
                        ScenarioEntitiesVm.UpdateEnemies(enemies);
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }, AwaitOperation.Drop);

        dataManager.DoorsObservable.ObserveOnThreadPool()
            .SubscribeAwait(async (doors, cancellationToken) =>
            {
                _logger.LogTrace("Processing doors data on thread pool");
                try
                {
                    await dispatcherService.InvokeOnUIAsync(() =>
                    {
                        ScenarioEntitiesVm.UpdateDoors(doors);
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }, AwaitOperation.Drop);
    }

    [RelayCommand]
    private Task ShowItemsDialogAsync()
    {
        return _dispatcherService.InvokeOnUIAsync(() =>
        {
            DialogManager.CreateDialog()
                .WithTitle("Scenario Items")
                .WithContent(new ScenarioItemsView { DataContext = ScenarioEntitiesVm })
                .WithActionButton("Close ", _ => { }, true)
                .TryShow();
        });
    }

    [RelayCommand]
    private Task ShowEnemiesDialogAsync()
    {
        return _dispatcherService.InvokeOnUIAsync(() =>
        {
            DialogManager.CreateDialog()
                .WithTitle("Scenario Enemies")
                .WithContent(new ScenarioEnemiesView { DataContext = ScenarioEntitiesVm })
                .WithActionButton("Close ", _ => { }, true)
                .TryShow();
        });
    }

    [RelayCommand]
    private Task ShowDoorsDialogAsync()
    {
        return _dispatcherService.InvokeOnUIAsync(() =>
        {
            DialogManager.CreateDialog()
                .WithTitle("Scenario Doors")
                .WithContent(new ScenarioDoorsView { DataContext = ScenarioEntitiesVm })
                .WithActionButton("Close ", _ => { }, true)
                .TryShow();
        });
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

        ScenarioEntitiesVm.UpdateItems(scenario.Items);

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
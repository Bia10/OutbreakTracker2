using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;
using OutbreakTracker2.Application.Views.GameDock;
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
    private string _scenarioName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScenarioActive))]
    [NotifyPropertyChangedFor(nameof(IsScenarioNotActive))]
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

    [ObservableProperty]
    private ScenarioEntitiesViewModel _scenarioEntitiesVm;

    public bool IsScenarioActive => FrameCounter > 0;
    public bool IsScenarioNotActive => FrameCounter <= 0;

    public InGameScenarioViewModel(
        ILogger<InGameScenarioViewModel> logger,
        IDataObservableSource dataObservable,
        IDispatcherService dispatcherService,
        ScenarioEntitiesViewModel scenarioEntitiesViewModel,
        ScenarioEntityCommands entityCommands
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _entityCommands = entityCommands;
        _scenarioEntitiesVm = scenarioEntitiesViewModel;
        _router = new ScenarioViewModelRouter();

        _disposables.Add(
            dataObservable
                .InGameScenarioObservable.WithLatestFrom(
                    dataObservable.InGamePlayersObservable,
                    (inGameScenario, players) => (Scenario: inGameScenario, Players: players)
                )
                .ObserveOnThreadPool()
                .SubscribeAwait(
                    async (data, cancellationToken) =>
                    {
                        _logger.LogTrace("Processing inGame scenario data on thread pool");
                        try
                        {
                            await dispatcherService
                                .InvokeOnUIAsync(
                                    () =>
                                    {
                                        _logger.LogTrace("Updating InGameScenarioViewModel properties on UI thread");
                                        Update(data.Scenario, data.Players);
                                    },
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("InGame scenario data processing cancelled");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during inGame scenario data processing cycle");
                        }
                    },
                    AwaitOperation.Drop
                )
        );

        _disposables.Add(
            dataObservable
                .EnemiesObservable.ObserveOnThreadPool()
                .SubscribeAwait(
                    async (enemies, cancellationToken) =>
                    {
                        _logger.LogTrace("Processing enemies data on thread pool");
                        try
                        {
                            await dispatcherService
                                .InvokeOnUIAsync(
                                    () =>
                                    {
                                        ScenarioEntitiesVm.UpdateEnemies(enemies);
                                    },
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Enemies data processing cancelled");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during enemies data processing cycle");
                        }
                    },
                    AwaitOperation.Drop
                )
        );

        _disposables.Add(
            dataObservable
                .DoorsObservable.ObserveOnThreadPool()
                .SubscribeAwait(
                    async (doors, cancellationToken) =>
                    {
                        _logger.LogTrace("Processing doors data on thread pool");
                        try
                        {
                            await dispatcherService
                                .InvokeOnUIAsync(
                                    () =>
                                    {
                                        ScenarioEntitiesVm.UpdateDoors(doors);
                                    },
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Doors data processing cancelled");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during doors data processing cycle");
                        }
                    },
                    AwaitOperation.Drop
                )
        );
    }

    public void Dispose() => _disposables.Dispose();

    public void Update(DecodedInGameScenario scenario, DecodedInGamePlayer[] players)
    {
        if (scenario.CurrentFile is < 1 or > 2)
        {
            FrameCounter = 0;
            ScenarioEntitiesVm.ClearItems();
            return;
        }

        CurrentFile = scenario.CurrentFile;
        ScenarioName = scenario.ScenarioName;
        FrameCounter = scenario.FrameCounter;
        Difficulty = scenario.Difficulty;
        Status = scenario.Status;
        PlayerCount = GetTrackedPlayerCount(players);
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

        ResolveItemDisplayFields(scenario, players);
        ScenarioEntitiesVm.UpdateItems(scenario.Items, scenario.FrameCounter, (GameFile)scenario.CurrentFile);

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

    private void ResolveItemDisplayFields(DecodedInGameScenario scenario, DecodedInGamePlayer[] players)
    {
        bool scenarioParsed = EnumUtility.TryParseByValueOrMember(ScenarioName, out Scenario scenarioEnum);

        for (int i = 0; i < scenario.Items.Length; i++)
        {
            DecodedItem item = scenario.Items[i];

            string roomName = scenarioParsed ? scenarioEnum.GetRoomName(item.RoomId) : $"Room {item.RoomId}";

            string pickedUpByName;
            if (item.PickedUp == 0)
            {
                pickedUpByName = "None";
            }
            else
            {
                int slotIndex = item.PickedUp - 1;
                if (slotIndex >= 0 && slotIndex < players.Length)
                {
                    DecodedInGamePlayer player = players[slotIndex];
                    pickedUpByName =
                        player.IsEnabled && !string.IsNullOrEmpty(player.Name) ? player.Name : $"P{item.PickedUp}";
                }
                else
                {
                    // Only warn when we have a valid player array — index 0 is a startup race
                    // where players haven't been decoded yet; those items are filtered by Present==0.
                    if (players.Length > 0)
                        _logger.LogWarning(
                            "Item slot={Slot} type={Type} has PickedUp={PickedUp} which is out of valid player range [1,{Max}]; Present={Present} Qty={Qty}",
                            item.SlotIndex,
                            item.TypeName,
                            item.PickedUp,
                            players.Length,
                            item.Present,
                            item.Quantity
                        );
                    pickedUpByName = $"P{item.PickedUp}";
                }
            }

            scenario.Items[i] = item with { RoomName = roomName, PickedUpByName = pickedUpByName };
        }
    }

    private void UpdateScenarioSpecificViewModel(DecodedInGameScenario scenario)
    {
        CurrentScenarioSpecificViewModel = _router.Route(scenario, _logger);
    }

    private string GetClearedDisplay() =>
        Status is ScenarioStatus.GameFinished or ScenarioStatus.RankScreen or ScenarioStatus.AfterSaveToMemoryCard
            ? "Yes"
            : "No";

    private string GetGameTime() => TimeUtility.GetTimeFromFrames(FrameCounter);

    private int CalculateGasRandomOrderDisplay() =>
        GasRandom switch
        {
            0 => -1,
            > 0 and < 240 => (GasRandom / 10) + 1,
            >= 240 and < 255 => 25,
            _ => -1,
        };
}

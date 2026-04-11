using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Common.ScenarioImg;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoomPlayer;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoom;

public sealed partial class LobbyRoomViewModel : ObservableObject, IAsyncDisposable
{
    private readonly ILogger<LobbyRoomViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly IDisposable _subscription;
    private readonly Dictionary<Ulid, LobbyRoomPlayerViewModel> _viewModelCache = [];
    private readonly ObservableList<LobbyRoomPlayerViewModel> _playersInternal = [];
    private byte _currentRoomMasterId = 255;

    public NotifyCollectionChangedSynchronizedViewList<LobbyRoomPlayerViewModel> PlayersView { get; }

    public ScenarioImageViewModel ScenarioImageViewModel { get; }

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _timeLeft = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayersDisplay))]
    private short _maxPlayer = GameConstants.MaxPlayers;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayersDisplay))]
    private short _curPlayer;

    [ObservableProperty]
    private string _difficulty = string.Empty;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    [ObservableProperty]
    private bool _isAtLobby;

    public string PlayersDisplay => $"{CurPlayer}/{MaxPlayer}";

    public LobbyRoomViewModel(
        IDataObservableSource dataObservable,
        ILogger<LobbyRoomViewModel> logger,
        IDispatcherService dispatcherService,
        ILobbyRoomPlayerViewModelFactory playerVmFactory,
        IScenarioImageViewModelFactory scenarioImageVmFactory
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        ScenarioImageViewModel = scenarioImageVmFactory.Create();

        PlayersView = _playersInternal.ToNotifyCollectionChanged(
            SynchronizationContextCollectionEventDispatcher.Current
        );

        IDisposable lobbyPresenceSubscription = dataObservable
            .IsAtLobbyObservable.ObserveOnThreadPool()
            .SubscribeAwait(
                async (isAtLobby, cancellationToken) =>
                {
                    await _dispatcherService
                        .InvokeOnUIAsync(() => IsAtLobby = isAtLobby, cancellationToken)
                        .ConfigureAwait(false);
                },
                AwaitOperation.Drop
            );

        IDisposable lobbyRoomDataSubscription = dataObservable
            .LobbyRoomObservable.WithLatestFrom(
                dataObservable.IsAtLobbyObservable,
                (lobbyData, isAtLobby) => (LobbyData: lobbyData, IsAtLobby: isAtLobby)
            )
            .Where(static state => state.IsAtLobby)
            .Select(static state => state.LobbyData)
            .ObserveOnThreadPool()
            .SubscribeAwait(
                async (lobbyData, cancellationToken) =>
                {
                    try
                    {
                        await _dispatcherService
                            .InvokeOnUIAsync(() => UpdateLobbyProperties(lobbyData), cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during lobby room data processing cycle");
                    }
                },
                AwaitOperation.Drop
            );

        IDisposable lobbyRoomPlayersSubscription = dataObservable
            .LobbyRoomPlayersObservable.WithLatestFrom(
                dataObservable.IsAtLobbyObservable,
                (players, isAtLobby) => (Players: players, IsAtLobby: isAtLobby)
            )
            .Where(static state => state.IsAtLobby)
            .Select(static state => state.Players)
            .ObserveOnThreadPool()
            .SubscribeAwait(
                async (incomingPlayersSnapshot, cancellationToken) =>
                {
                    List<DecodedLobbyRoomPlayer> filteredIncomingPlayers = incomingPlayersSnapshot
                        .AsValueEnumerable()
                        .Where(IsPlayerActive)
                        .ToList();

                    try
                    {
                        List<LobbyRoomPlayerViewModel> desiredViewModels = new(filteredIncomingPlayers.Count);
                        HashSet<Ulid> desiredVmUlids = [];
                        // AwaitOperation.Drop guarantees this callback does not overlap with the previous
                        // callback, so the thread-pool snapshot is taken after the previous UI mutation completed.
                        List<LobbyRoomPlayerViewModel> currentListSnapshot = [.. _playersInternal];

                        for (int i = 0; i < filteredIncomingPlayers.Count; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            DecodedLobbyRoomPlayer incomingData = filteredIncomingPlayers[i];
                            LobbyRoomPlayerViewModel vmToAddToList;

                            if (
                                i < currentListSnapshot.Count
                                && _viewModelCache.TryGetValue(
                                    currentListSnapshot[i].ViewModelId,
                                    out LobbyRoomPlayerViewModel? existingAndCachedVm
                                )
                            )
                            {
                                vmToAddToList = existingAndCachedVm;
                            }
                            else
                            {
                                vmToAddToList = playerVmFactory.Create(incomingData);
                            }

                            desiredViewModels.Add(vmToAddToList);
                            desiredVmUlids.Add(vmToAddToList.ViewModelId);
                        }

                        List<Ulid> vmUlidsToRemoveFromCache = [.. _viewModelCache.Keys.Except(desiredVmUlids)];

                        await _dispatcherService
                            .InvokeOnUIAsync(
                                () =>
                                {
                                    foreach (Ulid ulidToRemove in vmUlidsToRemoveFromCache)
                                        _viewModelCache.Remove(ulidToRemove, out LobbyRoomPlayerViewModel? _);

                                    if (desiredViewModels.Count != filteredIncomingPlayers.Count)
                                    {
                                        _logger.LogError(
                                            "Internal error: desiredViewModels count ({DesiredCount}) does not match incomingPlayersSnapshot length ({SnapshotLength})."
                                                + " Cannot update properties reliably",
                                            desiredViewModels.Count,
                                            incomingPlayersSnapshot.Length
                                        );
                                    }
                                    else
                                    {
                                        for (int i = 0; i < desiredViewModels.Count; i++)
                                        {
                                            LobbyRoomPlayerViewModel vm = desiredViewModels[i];
                                            DecodedLobbyRoomPlayer playerData = filteredIncomingPlayers[i];
                                            _viewModelCache.TryAdd(vm.ViewModelId, vm);
                                            vm.Update(playerData, _currentRoomMasterId);
                                        }
                                    }

                                    for (int i = 0; i < desiredViewModels.Count; i++)
                                    {
                                        LobbyRoomPlayerViewModel desiredVm = desiredViewModels[i];
                                        int currentIndexInList = -1;

                                        for (int j = 0; j < _playersInternal.Count; j++)
                                        {
                                            if (_playersInternal[j].ViewModelId == desiredVm.ViewModelId)
                                            {
                                                currentIndexInList = j;
                                                break;
                                            }
                                        }

                                        if (currentIndexInList is -1)
                                        {
                                            if (i <= _playersInternal.Count)
                                                _playersInternal.Insert(i, desiredVm);
                                            else
                                            {
                                                _logger.LogError(
                                                    "Internal error: Attempted to insert VM ULID {Ulid} at index {Index} which is out of bounds for list count {Count}",
                                                    desiredVm.ViewModelId,
                                                    i,
                                                    _playersInternal.Count
                                                );
                                                _playersInternal.Add(desiredVm);
                                            }
                                        }
                                        else if (currentIndexInList != i)
                                        {
                                            if (i >= 0 && i < _playersInternal.Count)
                                            {
                                                _playersInternal.Move(currentIndexInList, i);
                                            }
                                            else
                                            {
                                                _logger.LogError(
                                                    "Internal error: Attempted to move VM ULID {Ulid} from index {FromIndex} to invalid target index {ToIndex} for list count {Count}",
                                                    desiredVm.ViewModelId,
                                                    currentIndexInList,
                                                    i,
                                                    _playersInternal.Count
                                                );
                                            }
                                        }
                                    }

                                    for (int i = _playersInternal.Count - 1; i >= 0; i--)
                                    {
                                        LobbyRoomPlayerViewModel currentVmInList = _playersInternal[i];
                                        if (!desiredVmUlids.Contains(currentVmInList.ViewModelId))
                                            _playersInternal.RemoveAt(i);
                                    }

                                    if (_playersInternal.Count != desiredViewModels.Count)
                                        _logger.LogWarning(
                                            "_playersInternal count ({InternalCount}) differs from desiredViewModels count ({DesiredCount}) after sync. This indicates a potential sync logic error",
                                            _playersInternal.Count,
                                            desiredViewModels.Count
                                        );
                                },
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during lobby room players snapshot processing cycle");
                    }
                },
                AwaitOperation.Drop
            );

        _subscription = Disposable.Combine(
            lobbyPresenceSubscription,
            lobbyRoomDataSubscription,
            lobbyRoomPlayersSubscription
        );
    }

    private static bool IsPlayerActive(DecodedLobbyRoomPlayer player) =>
        !string.IsNullOrEmpty(player.CharacterName)
        || (!string.IsNullOrEmpty(player.NpcName) && player is { IsEnabled: true });

    private void UpdateLobbyProperties(in DecodedLobbyRoom model)
    {
        if (model.CurPlayer is <= 0 or > 4)
            return;

        Status = model.Status;
        TimeLeft = model.TimeLeft;
        MaxPlayer = model.MaxPlayer;
        CurPlayer = model.CurPlayer;
        Difficulty = model.Difficulty;
        ScenarioName = model.ScenarioName;
        _currentRoomMasterId = model.RoomMasterId;

        if (!string.IsNullOrEmpty(ScenarioName))
        {
            if (EnumUtility.TryParseByValueOrMember(ScenarioName, out Scenario scenarioType))
            {
                TrackScenarioImageUpdate(ScenarioImageViewModel.UpdateImageAsync(scenarioType), ScenarioName);
            }
            else
            {
                _logger.LogWarning(
                    "ScenarioName '{ScenarioName}' could not be parsed to a ScenarioType. Displaying default image",
                    ScenarioName
                );
                TrackScenarioImageUpdate(ScenarioImageViewModel.UpdateToDefaultImageAsync(), ScenarioName);
            }
        }
    }

    private void TrackScenarioImageUpdate(ValueTask updateTask, string scenarioName)
    {
        _ = updateTask
            .AsTask()
            .ContinueWith(
                task =>
                    _logger.LogError(
                        task.Exception,
                        "Failed to update scenario image for lobby room {ScenarioName}",
                        scenarioName
                    ),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default
            );
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing LobbyRoomViewModel asynchronously");

        _subscription.Dispose();

        await _dispatcherService
            .InvokeOnUIAsync(() =>
            {
                _playersInternal.Clear();
                _viewModelCache.Clear();
                PlayersView.Dispose();
                _logger.LogDebug("LobbyRoomViewModel collections cleared on UI thread during async dispose");
            })
            .ConfigureAwait(false);

        _logger.LogDebug("LobbyRoomViewModel asynchronous disposal complete");
    }
}

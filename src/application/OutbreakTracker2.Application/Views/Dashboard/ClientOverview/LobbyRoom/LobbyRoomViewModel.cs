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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZLinq;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoom;

public partial class LobbyRoomViewModel : ObservableObject, IAsyncDisposable
{
    private readonly ILogger<LobbyRoomViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly IDisposable _subscription;
    private readonly Dictionary<Ulid, LobbyRoomPlayerViewModel> _viewModelCache = [];
    private readonly ObservableList<LobbyRoomPlayerViewModel> _playersInternal = [];
    public NotifyCollectionChangedSynchronizedViewList<LobbyRoomPlayerViewModel> PlayersView { get; }

    public ScenarioImageViewModel ScenarioImageViewModel { get; }

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _timeLeft = string.Empty;

    [ObservableProperty]
    private short _maxPlayer = GameConstants.MaxPlayers;

    [ObservableProperty]
    private short _curPlayer;

    [ObservableProperty]
    private string _difficulty = string.Empty;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    public string PlayersDisplay => $"{CurPlayer}/{MaxPlayer}";

    public LobbyRoomViewModel(
        IDataManager dataManager,
        ILogger<LobbyRoomViewModel> logger,
        IDispatcherService dispatcherService,
        ILobbyRoomPlayerViewModelFactory playerVmFactory,
        IScenarioImageViewModelFactory scenarioImageVmFactory)
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _logger.LogInformation("Initializing LobbyRoomViewModel");
        ScenarioImageViewModel = scenarioImageVmFactory.Create();

        PlayersView =
            _playersInternal.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        IDisposable lobbyRoomDataSubscription = dataManager.LobbyRoomObservable.ObserveOnThreadPool()
            .SubscribeAwait(async (lobbyData, cancellationToken) =>
            {
                _logger.LogTrace("Processing lobby room data on thread pool");
                try
                {
                    await _dispatcherService.InvokeOnUIAsync(() =>
                        {
                            _logger.LogTrace("Updating LobbyRoomViewModel properties on UI thread");
                            UpdateLobbyProperties(lobbyData);
                        }, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Lobby room data processing cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during lobby room data processing cycle");
                }
            }, AwaitOperation.Drop);

        IDisposable lobbyRoomPlayersSubscription = dataManager.LobbyRoomPlayersObservable.ObserveOnThreadPool()
            .SubscribeAwait(async (incomingPlayersSnapshot, cancellationToken) =>
            {
                _logger.LogInformation(
                    "Processing lobby room players snapshot on thread pool with {Length} entries",
                    incomingPlayersSnapshot.Length);

                List<DecodedLobbyRoomPlayer> filteredIncomingPlayers = incomingPlayersSnapshot.AsValueEnumerable()
                    .Where(IsPlayerActive)
                    .ToList();

                _logger.LogInformation("Processed {Count} filtered player entries on thread pool",
                    filteredIncomingPlayers.Count);

                try
                {
                    List<LobbyRoomPlayerViewModel> desiredViewModels = new(filteredIncomingPlayers.Count);
                    HashSet<Ulid> desiredVmUlids = [];
                    List<LobbyRoomPlayerViewModel> currentListSnapshot;
                    try
                    {
                        currentListSnapshot = [.. _playersInternal];
                        _logger.LogTrace("Created snapshot of current UI list with {Count} items on ThreadPool",
                            currentListSnapshot.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error taking snapshot of _playersInternal on ThreadPool");
                        throw;
                    }

                    for (int i = 0; i < filteredIncomingPlayers.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        DecodedLobbyRoomPlayer incomingData = filteredIncomingPlayers[i];
                        LobbyRoomPlayerViewModel vmToAddToList;

                        if (i < currentListSnapshot.Count && _viewModelCache.TryGetValue(
                                currentListSnapshot[i].ViewModelId,
                                out LobbyRoomPlayerViewModel? existingAndCachedVm))
                        {
                            vmToAddToList = existingAndCachedVm;
                            _logger.LogTrace("Reusing existing VM from cache {Ulid} for incoming data at index {Index} (PlayerId {PlayerId})",
                                vmToAddToList.ViewModelId, i, incomingData.NameId);
                        }
                        else
                        {
                            vmToAddToList = playerVmFactory.Create(incomingData);
                            _logger.LogDebug("Creating new VM {Ulid} for incoming data at index {Index} (PlayerId {PlayerId})",
                                vmToAddToList.ViewModelId, i, incomingData.NameId);
                        }

                        desiredViewModels.Add(vmToAddToList);
                        desiredVmUlids.Add(vmToAddToList.ViewModelId);
                    }

                    List<Ulid> vmUlidsToRemoveFromCache = _viewModelCache.Keys.Except(desiredVmUlids)
                        .ToList();

                    _logger.LogInformation("Player ViewModel preparation complete on thread pool. {DesiredCount} desired VMs. {RemovedCount} VMs to potentially remove from cache",
                        desiredViewModels.Count, vmUlidsToRemoveFromCache.Count);

                    await _dispatcherService.InvokeOnUIAsync(() =>
                        {
                            _logger.LogInformation("Applying player updates and list synchronization on UI thread");

                            foreach (Ulid ulidToRemove in vmUlidsToRemoveFromCache)
                            {
                                if (_viewModelCache.Remove(ulidToRemove, out LobbyRoomPlayerViewModel? _))
                                {
                                    _logger.LogDebug("Removing VM from cache on UI for ULID {Ulid}", ulidToRemove);
                                }
                                else
                                {
                                    _logger.LogWarning("Attempted to remove VM with ULID {Ulid} from cache but it was not found",
                                        ulidToRemove);
                                }
                            }

                            _logger.LogTrace("Cache count after removals: {Count}", _viewModelCache.Count);

                            if (desiredViewModels.Count != filteredIncomingPlayers.Count)
                            {
                                _logger.LogError("Internal error: desiredViewModels count ({DesiredCount}) does not match incomingPlayersSnapshot length ({SnapshotLength})." +
                                                 " Cannot update properties reliably", desiredViewModels.Count, incomingPlayersSnapshot.Length);
                            }
                            else
                            {
                                for (int i = 0; i < desiredViewModels.Count; i++)
                                {
                                    LobbyRoomPlayerViewModel vm = desiredViewModels[i];
                                    DecodedLobbyRoomPlayer playerData = filteredIncomingPlayers[i];

                                    if (_viewModelCache.TryAdd(vm.ViewModelId, vm))
                                    {
                                        _logger.LogTrace("Adding new VM to cache on UI thread for ULID {Ulid} (PlayerId {PlayerId})", vm.ViewModelId, vm.DataPlayerId);
                                    }

                                    vm.Update(playerData);
                                    _logger.LogTrace("Updating LobbyRoomPlayerViewModel properties on UI thread for ULID {Ulid} (PlayerId {PlayerId})",
                                        vm.ViewModelId, vm.DataPlayerId);
                                }

                                _logger.LogTrace("Properties updated for {Count} VMs", desiredViewModels.Count);
                            }

                            _logger.LogTrace("Cache count after additions/updates: {Count}", _viewModelCache.Count);

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
                                    _logger.LogDebug("Inserting VM into UI list: ULID {Ulid} at index {Index}", desiredVm.ViewModelId, i);

                                    if (i <= _playersInternal.Count)
                                        _playersInternal.Insert(i, desiredVm);
                                    else
                                    {
                                        _logger.LogError("Internal error: Attempted to insert VM ULID {Ulid} at index {Index} which is out of bounds for list count {Count}",
                                            desiredVm.ViewModelId, i, _playersInternal.Count);
                                        _playersInternal.Add(desiredVm); // Fallback add
                                    }
                                }
                                else if (currentIndexInList != i)
                                {
                                    _logger.LogDebug("Moving VM in UI list: ULID {Ulid} from index {FromIndex} to index {ToIndex}", desiredVm.ViewModelId, currentIndexInList, i);

                                    if (i >= 0 && i < _playersInternal.Count)
                                    {
                                        _playersInternal.Move(currentIndexInList, i);
                                    }
                                    else
                                    {
                                        _logger.LogError("Internal error: Attempted to move VM ULID {Ulid} from index {FromIndex} to invalid target index {ToIndex} for list count {Count}",
                                            desiredVm.ViewModelId, currentIndexInList, i, _playersInternal.Count);
                                    }
                                }
                                else
                                {
                                    _logger.LogTrace("VM ULID {Ulid} already in correct position {Index}", desiredVm.ViewModelId, i);
                                }
                            }

                            _logger.LogTrace("UI list count after moves/inserts: {Count}", _playersInternal.Count);


                            for (int i = _playersInternal.Count - 1; i >= 0; i--)
                            {
                                LobbyRoomPlayerViewModel currentVmInList = _playersInternal[i];
                                if (!desiredVmUlids.Contains(currentVmInList.ViewModelId))
                                {
                                    _logger.LogDebug("Removing VM from UI list: ULID {Ulid}", currentVmInList.ViewModelId);
                                    _playersInternal.RemoveAt(i);
                                }
                            }

                            _logger.LogTrace("UI list count after removals: {Count}", _playersInternal.Count);

                            if (_playersInternal.Count != desiredViewModels.Count)
                                _logger.LogWarning(
                                    "_playersInternal count ({InternalCount}) differs from desiredViewModels count ({DesiredCount}) after sync. This indicates a potential sync logic error",
                                    _playersInternal.Count, desiredViewModels.Count);

                            _logger.LogInformation("UI update complete. Players UI list count: {Count}", _playersInternal.Count);
                        }, cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogInformation("Finished processing lobby room players snapshot cycle");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Lobby room players snapshot processing cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during lobby room players snapshot processing cycle");
                }
            }, AwaitOperation.Drop);

        _subscription = Disposable.Combine(lobbyRoomDataSubscription, lobbyRoomPlayersSubscription);
    }

    private static bool IsPlayerActive(DecodedLobbyRoomPlayer player)
        => !string.IsNullOrEmpty(player.CharacterName)
           || (!string.IsNullOrEmpty(player.NpcName)
               && player is { IsEnabled: true });

    private void UpdateLobbyProperties(DecodedLobbyRoom model)
    {
        if (model.CurPlayer is <= 0 or > 4) return;

        Status = model.Status;
        TimeLeft = model.TimeLeft;
        MaxPlayer = model.MaxPlayer;
        CurPlayer = model.CurPlayer;
        Difficulty = model.Difficulty;
        ScenarioName = model.ScenarioName;

        if (!string.IsNullOrEmpty(ScenarioName))
        {
            if (EnumUtility.TryParseByValueOrMember(ScenarioName, out Scenario scenarioType))
            {
                _ = ScenarioImageViewModel.UpdateImageAsync(scenarioType);
            }
            else
            {
                _logger.LogWarning("ScenarioName '{ScenarioName}' could not be parsed to a ScenarioType. Displaying default image", ScenarioName);
                _ = ScenarioImageViewModel.UpdateToDefaultImageAsync();
            }
        }

        OnPropertyChanged(nameof(PlayersDisplay));
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing LobbyRoomViewModel asynchronously");

        _subscription.Dispose();

        await _dispatcherService.InvokeOnUIAsync(() =>
        {
            _playersInternal.Clear();
            _viewModelCache.Clear();
            _logger.LogDebug("LobbyRoomViewModel collections cleared on UI thread during async dispose");
        }).ConfigureAwait(false);

        _logger.LogDebug("LobbyRoomViewModel asynchronous disposal complete");
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlot;
using OutbreakTracker2.Outbreak.Models;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlots;

// NOTE: Currently this relies on weird magic:
// The array index is of incoming data is used to pair with ULID of existing VMs.
// This is not ideal, but it works for now.
// The assumption is that the incoming data is in the same order as the VMs in the cache.
public class LobbySlotsViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IDisposable _subscription;
    private readonly ILogger<LobbySlotsViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly ObservableDictionary<Ulid, LobbySlotViewModel> _viewModelCache = [];
    private readonly ObservableList<LobbySlotViewModel> _lobbySlotsInternal = [];

    public NotifyCollectionChangedSynchronizedViewList<LobbySlotViewModel> LobbySlotsView { get; }

    public LobbySlotsViewModel(
        IDataManager dataManager,
        ILogger<LobbySlotsViewModel> logger,
        IDispatcherService dispatcherService)
    {
        _logger = logger;
        _dispatcherService = dispatcherService;

        LobbySlotsView = _lobbySlotsInternal
            .ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = dataManager.LobbySlotsObservable
            .ObserveOnThreadPool()
            .SubscribeAwait(async (lobbySlotsSnapshot, cancellationToken) =>
            {
                _logger.LogInformation("Processing lobby slots snapshot on thread pool with {Length} entries", lobbySlotsSnapshot.Length);

                try
                {
                    List<LobbySlotViewModel> desiredViewModels = new(lobbySlotsSnapshot.Length);
                    HashSet<Ulid> activeVmUlids = [];

                    Dictionary<int, DecodedLobbySlot> incomingDataByIndex = new(lobbySlotsSnapshot.Length);
                    for (int i = 0; i < lobbySlotsSnapshot.Length; i++)
                    {
                        incomingDataByIndex[i] = lobbySlotsSnapshot[i];
                    }

                    for (int i = 0; i < lobbySlotsSnapshot.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        DecodedLobbySlot incomingData = lobbySlotsSnapshot[i];
                        LobbySlotViewModel vmToAddToList;

                        List<LobbySlotViewModel> currentListSnapshot;
                        try
                        {
                            currentListSnapshot = [.. _lobbySlotsInternal];
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error taking snapshot of _lobbySlotsInternal on ThreadPool");
                            throw;
                        }

                        if (i < currentListSnapshot.Count)
                        {
                            LobbySlotViewModel potentialVmToReuse = currentListSnapshot[i];
                            if (_viewModelCache.TryGetValue(potentialVmToReuse.Id, out LobbySlotViewModel? existingAndCachedVm))
                            {
                                vmToAddToList = existingAndCachedVm;
                                _logger.LogTrace("Reusing existing VM {Ulid} for index {Index}", vmToAddToList.Id, i);
                            }
                            else
                            {
                                _logger.LogWarning("VM {Ulid} from old list snapshot at index {Index} not found in cache. Creating new", potentialVmToReuse.Id, i);
                                vmToAddToList = new LobbySlotViewModel(incomingData);
                            }
                        }
                        else
                        {
                            vmToAddToList = new LobbySlotViewModel(incomingData);
                            _logger.LogDebug("Creating new VM {Ulid} for new index {Index}", vmToAddToList.Id, i);
                        }

                        desiredViewModels.Add(vmToAddToList);
                        activeVmUlids.Add(vmToAddToList.Id);
                    }

                    IReadOnlyDictionary<Ulid, LobbySlotViewModel> cacheAsKvpDict = _viewModelCache;
                    List<Ulid> ulidsToRemove = cacheAsKvpDict.Keys.Except(activeVmUlids).ToList();

                    _logger.LogInformation("ViewModel preparation complete on thread pool. {DesiredCount} desired VMs. {RemovedCount} VMs to remove", desiredViewModels.Count, ulidsToRemove.Count);

                    await _dispatcherService.InvokeOnUIAsync(() =>
                    {
                        _logger.LogInformation("Applying lobby slot updates on UI thread");

                        foreach (Ulid ulid in ulidsToRemove)
                        {
                            if (_viewModelCache.Remove(ulid))
                            {
                                _logger.LogDebug("Removing VM from cache on UI thread for ULID: {Ulid}", ulid);
                            }
                            else
                            {
                                _logger.LogWarning("Attempted to remove VM {Ulid} from cache but it was not found", ulid);
                            }
                        }

                        foreach (LobbySlotViewModel vm in desiredViewModels)
                        {
                            if (_viewModelCache.ContainsKey(vm.Id))
                                continue;

                            _viewModelCache[vm.Id] = vm;
                            _logger.LogTrace("Adding new VM to cache on UI thread for ULID: {Ulid}", vm.Id);
                        }

                        _logger.LogTrace("Cache count after additions: {Count}", _viewModelCache.Count);

                        for (int i = 0; i < desiredViewModels.Count; i++)
                        {
                            LobbySlotViewModel vm = desiredViewModels[i];
                            DecodedLobbySlot incomingData = incomingDataByIndex[i];

                            vm.Update(incomingData);
                            _logger.LogTrace("Updating VM properties on UI thread for ULID {Ulid} (Slot {Slot})", vm.Id, incomingData.SlotNumber); // Use VM Ulid and data SlotNumber for logging
                        }

                        _logger.LogTrace("Properties updated for {Count} VMs", desiredViewModels.Count);

                        HashSet<Ulid> desiredVmUlidsLookup = new(desiredViewModels.Select(vm => vm.Id));
                        for (int i = _lobbySlotsInternal.Count - 1; i >= 0; i--)
                        {
                            LobbySlotViewModel currentVmInList = _lobbySlotsInternal[i];
                            if (desiredVmUlidsLookup.Contains(currentVmInList.Id))
                                continue;

                            _logger.LogDebug("Removing VM from UI list: ULID {Ulid}", currentVmInList.Id);
                            _lobbySlotsInternal.RemoveAt(i);
                        }

                        _logger.LogTrace("UI list count after removals: {Count}", _lobbySlotsInternal.Count);


                        for (int i = 0; i < desiredViewModels.Count; i++)
                        {
                            LobbySlotViewModel desiredVm = desiredViewModels[i];
                            int currentIndexInList = -1;

                            for (int j = 0; j < _lobbySlotsInternal.Count; j++)
                            {
                                if (_lobbySlotsInternal[j].Id != desiredVm.Id)
                                    continue;

                                currentIndexInList = j;
                                break;
                            }

                            if (currentIndexInList is -1)
                            {
                                _logger.LogDebug("Inserting VM into UI list: ULID {Ulid} at index {Index}", desiredVm.Id, i);

                                if (i <= _lobbySlotsInternal.Count)
                                    _lobbySlotsInternal.Insert(i, desiredVm);
                                else
                                    _lobbySlotsInternal.Add(desiredVm);
                            }
                            else if (currentIndexInList != i)
                            {
                                _logger.LogDebug("Moving VM in UI list: ULID {Ulid} from index {FromIndex} to index {ToIndex}", desiredVm.Id, currentIndexInList, i);
                                _lobbySlotsInternal.Move(currentIndexInList, i);
                            }
                            else
                            {
                                _logger.LogTrace("VM {Ulid} already in correct position {Index}", desiredVm.Id, i);
                            }
                        }

                        _logger.LogTrace("UI list count after adds/moves: {Count}", _lobbySlotsInternal.Count);

                        if (_lobbySlotsInternal.Count != desiredViewModels.Count)
                            _logger.LogWarning("_lobbySlotsInternal count ({InternalCount}) differs from desiredViewModels count ({DesiredCount}) after sync. This indicates a potential sync logic error",
                                _lobbySlotsInternal.Count, desiredViewModels.Count);

                        _logger.LogInformation("UI update complete. LobbySlots UI list count: {Count}", _lobbySlotsInternal.Count);
                    }, cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Finished processing lobby slot snapshot cycle");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Lobby slot snapshot processing cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during lobby slot snapshot processing cycle");
                }
            }, AwaitOperation.Drop);
    }

    public async ValueTask DisposeAsync()
    {
        // We assume that singleton view/vm is not disposed multiple times.

        _logger.LogDebug("Disposing LobbySlotsViewModel asynchronously");

        _subscription.Dispose();

        await _dispatcherService.InvokeOnUIAsync(() =>
        {
            _lobbySlotsInternal.Clear();
            _viewModelCache.Clear();
            _logger.LogDebug("LobbySlotsViewModel collections cleared on UI thread during async dispose");
        }).ConfigureAwait(false);

        _logger.LogDebug("LobbySlotsViewModel asynchronous disposal complete");
    }
}
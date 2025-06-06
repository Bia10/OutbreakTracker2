using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot.Factory;
using OutbreakTracker2.Outbreak.Models;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlots;

// NOTE: Currently this relies on weird magic:
// The array index of incoming data is used to pair with ULID of existing VMs.
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
        IDispatcherService dispatcherService,
        ILobbySlotViewModelFactory lobbySlotVmFactory)
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
                    List<LobbySlotViewModel> desiredViewModelsForUiList = new(lobbySlotsSnapshot.Length);
                    HashSet<Ulid> activeIncomingUlids = [];

                    for (int i = 0; i < lobbySlotsSnapshot.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        DecodedLobbySlot incomingData = lobbySlotsSnapshot[i];
                        LobbySlotViewModel vmToUse;

                        if (i < _lobbySlotsInternal.Count)
                        {
                            LobbySlotViewModel currentVmInList = _lobbySlotsInternal[i];
                            if (_viewModelCache.TryGetValue(currentVmInList.Id, out LobbySlotViewModel? cachedVm))
                            {
                                vmToUse = cachedVm;
                                _logger.LogTrace("Reusing cached VM {Ulid} for index {Index}", vmToUse.Id, i);
                            }
                            else
                            {
                                vmToUse = lobbySlotVmFactory.Create(incomingData);
                                _logger.LogWarning("VM {Ulid} from old list snapshot at index {Index} not found in cache or its ULID changed. Creating new VM for incoming data", currentVmInList.Id, i);
                            }
                        }
                        else
                        {
                            vmToUse = lobbySlotVmFactory.Create(incomingData);
                            _logger.LogDebug("Creating new VM {Ulid} for new index {Index}", vmToUse.Id, i);
                        }

                        vmToUse.Update(incomingData);
                        desiredViewModelsForUiList.Add(vmToUse);
                        activeIncomingUlids.Add(vmToUse.Id);
                    }

                    IReadOnlyDictionary<Ulid, LobbySlotViewModel> cacheAsKvpDict = _viewModelCache;
                    List<Ulid> ulidsToRemoveFromCache = cacheAsKvpDict.Keys
                        .Except(activeIncomingUlids)
                        .ToList();

                    _logger.LogInformation(
                        "ViewModel preparation complete on thread pool. {DesiredCount} desired VMs. {RemovedCount} VMs to remove from cache",
                        desiredViewModelsForUiList.Count, ulidsToRemoveFromCache.Count);

                    await _dispatcherService.InvokeOnUIAsync(() =>
                    {
                        _logger.LogInformation("Applying lobby slot updates on UI thread");

                        foreach (Ulid ulid in ulidsToRemoveFromCache)
                        {
                            if (_viewModelCache.Remove(ulid))
                                _logger.LogDebug("Removing VM from cache on UI thread for ULID: {Ulid}", ulid);
                            else
                                _logger.LogWarning("Attempted to remove VM {Ulid} from cache but it was not found", ulid);
                        }

                        foreach (LobbySlotViewModel vm in desiredViewModelsForUiList)
                        {
                            if (!_viewModelCache.ContainsKey(vm.Id))
                            {
                                _viewModelCache[vm.Id] = vm;
                                _logger.LogTrace("Adding new VM to cache on UI thread for ULID: {Ulid}", vm.Id);
                            }
                        }

                        _logger.LogTrace("Cache count after additions: {Count}", _viewModelCache.Count);

                        for (int i = _lobbySlotsInternal.Count - 1; i >= 0; i--)
                        {
                            LobbySlotViewModel currentVmInList = _lobbySlotsInternal[i];
                            if (!activeIncomingUlids.Contains(currentVmInList.Id))
                            {
                                _logger.LogDebug("Removing VM from UI list: ULID {Ulid} at index {Index} (no longer active)",
                                    currentVmInList.Id, i);
                                _lobbySlotsInternal.RemoveAt(i);
                            }
                        }

                        for (int i = 0; i < desiredViewModelsForUiList.Count; i++)
                        {
                            LobbySlotViewModel desiredVm = desiredViewModelsForUiList[i];
                            int currentIndexInList = _lobbySlotsInternal.IndexOf(desiredVm);

                            if (currentIndexInList == -1)
                            {
                                _logger.LogDebug("Inserting VM into UI list: ULID {Ulid} at index {Index}",
                                    desiredVm.Id, i);

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
                        }

                        _logger.LogTrace("UI list count after adds/moves/removals: {Count}", _lobbySlotsInternal.Count);

                        if (_lobbySlotsInternal.Count != desiredViewModelsForUiList.Count)
                            _logger.LogWarning("_lobbySlotsInternal count ({InternalCount}) differs from desiredViewModels count ({DesiredCount}) after sync. This indicates a potential sync logic error",
                                _lobbySlotsInternal.Count, desiredViewModelsForUiList.Count);

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
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
using ZLinq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbySlots;

public partial class LobbySlotsViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly ILogger<LobbySlotsViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly IDataManager _dataManager;
    private readonly Dictionary<short, LobbySlotViewModel> _viewModelCache = [];

    private readonly ObservableList<LobbySlotViewModel> _lobbySlotsInternal = [];
    public NotifyCollectionChangedSynchronizedViewList<LobbySlotViewModel> LobbySlots { get; }

    public LobbySlotsViewModel(
        IDataManager dataManager, 
        ILogger<LobbySlotsViewModel> logger, 
        IDispatcherService dispatcherService)
    {
        _dataManager = dataManager;
        _logger = logger;
        _dispatcherService = dispatcherService;
        _logger.LogInformation("Initializing LobbySlotsViewModel");

        LobbySlots = _lobbySlotsInternal
            .ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = dataManager.LobbySlotsObservable
            .ObserveOnThreadPool()
            .SubscribeAwait(async (lobbySlotsSnapshot, cancellationToken) =>
                {
                    _logger.LogInformation("Processing lobby slots snapshot on thread pool with {Length} entries", lobbySlotsSnapshot.Length);

                    try
                    {
                        var filteredIncomingSlots = lobbySlotsSnapshot
                            .AsValueEnumerable() 
                            .ToList();

                        _logger.LogInformation("Processed {Count} filtered lobby slot entries on thread pool.", filteredIncomingSlots.Count);

                        var incomingSlotDataMap = new Dictionary<short, DecodedLobbySlot>(filteredIncomingSlots.Count);
                        foreach (var slot in filteredIncomingSlots.AsValueEnumerable())
                        {
                            if (!incomingSlotDataMap.TryAdd(slot.SlotNumber, slot))
                            {
                                _logger.LogWarning("Duplicate SlotNumber '{SlotNumber}' found in incoming lobby slots snapshot. Ignoring this duplicate entry and keeping the first one encountered.", slot.SlotNumber);
                            }
                        }

                        var desiredViewModels = new List<LobbySlotViewModel>(filteredIncomingSlots.Count);

                        foreach (DecodedLobbySlot incomingSlotData in filteredIncomingSlots.AsValueEnumerable())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            short slotNumberKey = incomingSlotData.SlotNumber;

                            if (_viewModelCache.TryGetValue(slotNumberKey, out LobbySlotViewModel? existingVm))
                            {
                                desiredViewModels.Add(existingVm);
                                _logger.LogTrace("Found existing LobbySlotViewModel in cache on TP for SlotNumber {SlotNumber}", slotNumberKey);
                            }
                            else
                            {
                                _logger.LogDebug("Creating new LobbySlotViewModel on TP for SlotNumber {SlotNumber}", slotNumberKey);
                                var newVm = new LobbySlotViewModel(incomingSlotData); 
                                desiredViewModels.Add(newVm);
                            }
                        }

                        desiredViewModels.Sort((vm1, vm2) => vm1.SlotNumber.CompareTo(vm2.SlotNumber));

                        _logger.LogInformation("ViewModel preparation complete on thread pool. {DesiredCount} desired VMs.", desiredViewModels.Count);

                        await _dispatcherService.InvokeOnUIAsync(() =>
                        {
                            _logger.LogInformation("Applying lobby slot updates on UI thread.");

                            foreach (LobbySlotViewModel vm in desiredViewModels)
                            {
                                if (incomingSlotDataMap.TryGetValue(vm.UniqueSlotId, out DecodedLobbySlot? slotData))
                                {
                                    vm.Update(slotData);
                                    _logger.LogTrace("Updating LobbySlotViewModel properties on UI thread for SlotNumber {SlotNumber}", vm.UniqueSlotId);
                                }
                                else
                                {
                                    _logger.LogWarning("Slot data not found in map for VM SlotNumber {SlotNumber} during UI update.", vm.UniqueSlotId);
                                }
                            }

                            var desiredSlotNumbersLookup = new HashSet<short>(desiredViewModels.Select(vm => vm.UniqueSlotId));
                            for (int i = _lobbySlotsInternal.Count - 1; i >= 0; i--)
                            {
                                LobbySlotViewModel currentVmInList = _lobbySlotsInternal[i];
                                if (desiredSlotNumbersLookup.Contains(currentVmInList.UniqueSlotId))
                                {
                                    continue;
                                }

                                _logger.LogDebug("Removing LobbySlotViewModel from UI list & Cache for SlotNumber: {SlotNumber}", currentVmInList.UniqueSlotId);
                                _lobbySlotsInternal.RemoveAt(i);
                                _viewModelCache.Remove(currentVmInList.UniqueSlotId);
                            }

                            for (int i = 0; i < desiredViewModels.Count; i++)
                            {
                                LobbySlotViewModel desiredVm = desiredViewModels[i];

                                if (_viewModelCache.TryAdd(desiredVm.UniqueSlotId, desiredVm))
                                {
                                    _logger.LogDebug("Added new LobbySlotViewModel to cache on UI thread: SlotNumber {SlotNumber}", desiredVm.UniqueSlotId);
                                }

                                int currentIndexInPlayersList = -1;
                                for(int j=0; j < _lobbySlotsInternal.Count; j++)
                                {
                                    if(_lobbySlotsInternal[j].UniqueSlotId == desiredVm.UniqueSlotId)
                                    {
                                        currentIndexInPlayersList = j;
                                        break;
                                    }
                                }

                                if (currentIndexInPlayersList == -1)
                                {
                                    _logger.LogDebug("Inserting LobbySlotViewModel into UI list on UI thread: SlotNumber {SlotNumber} at index {Index}", desiredVm.UniqueSlotId, i);
                                    if (i <= _lobbySlotsInternal.Count)
                                        _lobbySlotsInternal.Insert(i, desiredVm);
                                }
                                else if (currentIndexInPlayersList != i)
                                {
                                    if (i == _lobbySlotsInternal.Count)
                                    {
                                        _logger.LogDebug("Removing LobbySlotViewModel from current position {FromIndex} and re-inserting at end ({ToIndex}) for SlotNumber: {SlotNumber}", currentIndexInPlayersList, i, desiredVm.UniqueSlotId);
                                        _lobbySlotsInternal.RemoveAt(currentIndexInPlayersList);
                                        if (i <= _lobbySlotsInternal.Count)
                                            _lobbySlotsInternal.Insert(i, desiredVm);
                                    }
                                    else
                                    {
                                        _logger.LogDebug("Moving LobbySlotViewModel in UI list on UI thread: SlotNumber {SlotNumber} from index {FromIndex} to index {ToIndex}", desiredVm.UniqueSlotId, currentIndexInPlayersList, i);
                                        _lobbySlotsInternal.Move(currentIndexInPlayersList, i);
                                    }
                                }
                                else
                                {
                                    _logger.LogTrace("LobbySlotViewModel {SlotNumber} already in correct position {Index}.", desiredVm.UniqueSlotId, i);
                                }
                            }

                            if (_lobbySlotsInternal.Count != desiredViewModels.Count)
                            {
                                _logger.LogWarning("_lobbySlotsInternal count ({InternalCount}) differs from desiredViewModels count ({DesiredCount}) after sync. This indicates a potential sync logic error.", _lobbySlotsInternal.Count, desiredViewModels.Count);
                            }

                            _logger.LogInformation("UI update complete. LobbySlots UI list count: {Count}", _lobbySlotsInternal.Count);
                        }, cancellationToken);

                        _logger.LogInformation("Finished processing lobby slot snapshot cycle.");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Lobby slot snapshot processing cancelled.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during lobby slot snapshot processing cycle.");
                    }
                },
                AwaitOperation.Drop);
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing LobbySlotsViewModel.");
        _subscription.Dispose();

        _dispatcherService.PostOnUI(() =>
        {
            _lobbySlotsInternal.Clear();
            _viewModelCache.Clear();
            _logger.LogDebug("LobbySlotsViewModel collections cleared on UI thread during dispose.");
        });

        _logger.LogDebug("LobbySlotsViewModel disposal complete.");
    }
}
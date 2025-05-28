using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameDoor;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Models;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameDoors;

public class InGameDoorsViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly ILogger<InGameDoorsViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly Dictionary<Ulid, InGameDoorViewModel> _viewModelCache = [];

    private ObservableList<InGameDoorViewModel> Doors { get; } = [];
    public NotifyCollectionChangedSynchronizedViewList<InGameDoorViewModel> DoorsView { get; }

    public InGameDoorsViewModel(
        IDataManager dataManager,
        ILogger<InGameDoorsViewModel> logger,
        IDispatcherService dispatcherService)
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _logger.LogInformation("Initializing InGameDoorsViewModel");

        DoorsView = Doors.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = dataManager.DoorsObservable
            .ObserveOnThreadPool()
            .SubscribeAwait(async (incomingDoorsSnapshot, ct) =>
            {
                switch (incomingDoorsSnapshot.Length)
                {
                    case 0: _logger.LogWarning("Received empty or null doors snapshot. Entries {Length}", incomingDoorsSnapshot.Length); return;
                    case > GameConstants.MaxDoors: _logger.LogWarning("Received more doors then is the inGame limit. Entries {Length}", incomingDoorsSnapshot.Length); return;
                    default: _logger.LogInformation("Processing doors snapshot on thread pool with {Length} entries", incomingDoorsSnapshot.Length);
                        try
                        {
                            List<DecodedDoor> filteredIncomingDoors = incomingDoorsSnapshot
                                .AsValueEnumerable()
                                // .Where(...)
                                .ToList();

                            _logger.LogInformation("Processed {Count} filtered door entries on thread pool", filteredIncomingDoors.Count);

                            Dictionary<Ulid, DecodedDoor> incomingDoorDataMap = filteredIncomingDoors
                                .AsValueEnumerable()
                                .ToDictionary(door => door.Id);

                            List<InGameDoorViewModel> desiredViewModels = new(filteredIncomingDoors.Count);

                            foreach (DecodedDoor incomingDoor in filteredIncomingDoors.AsValueEnumerable())
                            {
                                ct.ThrowIfCancellationRequested();

                                if (_viewModelCache.TryGetValue(incomingDoor.Id, out InGameDoorViewModel? existingVm))
                                {
                                    desiredViewModels.Add(existingVm);
                                    _logger.LogTrace("Found existing ViewModel in cache on TP for {UniqueId}", incomingDoor.Id);
                                }
                                else
                                {
                                    _logger.LogDebug("Creating new ViewModel on TP for {UniqueId}", incomingDoor.Id);
                                    InGameDoorViewModel newVm = new(incomingDoor);
                                    desiredViewModels.Add(newVm);
                                }
                            }

                            _logger.LogInformation("ViewModel preparation complete on thread pool. {DesiredCount} desired VMs", desiredViewModels.Count);

                            await _dispatcherService.InvokeOnUIAsync(() =>
                            {
                                _logger.LogInformation("Applying door updates on UI thread");

                                foreach (InGameDoorViewModel vm in desiredViewModels)
                                {
                                    if (incomingDoorDataMap.TryGetValue(vm.UniqueId, out DecodedDoor? doorData))
                                    {
                                        vm.Update(doorData);
                                        _viewModelCache.TryAdd(vm.UniqueId, vm);
                                        _logger.LogTrace("Updating ViewModel properties and adding to cache on UI thread for {UniqueId}", vm.UniqueId);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Door data not found in map for VM {UniqueId} during UI update. This should not happen", vm.UniqueId);
                                    }
                                }

                                HashSet<Ulid> desiredUniqueIdsLookup = new(desiredViewModels.Select(vm => vm.UniqueId));
                                for (int i = Doors.Count - 1; i >= 0; i--)
                                {
                                    InGameDoorViewModel currentVmInList = Doors[i];
                                    if (desiredUniqueIdsLookup.Contains(currentVmInList.UniqueId))
                                        continue;

                                    _logger.LogDebug("Removing ViewModel from Doors list & Cache on UI thread for UniqueId: {UniqueId}", currentVmInList.UniqueId);
                                    Doors.RemoveAt(i);
                                    _viewModelCache.Remove(currentVmInList.UniqueId);
                                }

                                for (int i = 0; i < desiredViewModels.Count; i++)
                                {
                                    InGameDoorViewModel desiredVm = desiredViewModels[i];

                                    int currentIndexInDoors;
                                    if (i < Doors.Count && Doors[i].UniqueId.Equals(desiredVm.UniqueId))
                                    {
                                        currentIndexInDoors = i;
                                        _logger.LogTrace("ViewModel {UniqueId} already in correct position", desiredVm.UniqueId);
                                    }
                                    else
                                    {
                                        currentIndexInDoors = Doors.IndexOf(desiredVm);
                                    }

                                    if (currentIndexInDoors is -1)
                                    {
                                        _logger.LogDebug("Inserting ViewModel into ObservableList on UI thread: {UniqueId} at index {Index}", desiredVm.UniqueId, i);
                                        Doors.Insert(i, desiredVm);
                                    }
                                    else if (currentIndexInDoors != i)
                                    {
                                        _logger.LogDebug("Moving ViewModel in ObservableList on UI thread: {UniqueId} from index {FromIndex} to index {ToIndex}", desiredVm.UniqueId, currentIndexInDoors, i);
                                        Doors.Move(currentIndexInDoors, i);
                                    }
                                }

                                if (Doors.Count != desiredViewModels.Count)
                                    _logger.LogWarning("_doors count ({DCount}) differs from desiredViewModels count ({DesCount}) after sync. Adjusting", Doors.Count, desiredViewModels.Count);

                                _logger.LogInformation("UI update complete. Doors ObservableList count: {Count}", Doors.Count);
                            }, ct);

                            _logger.LogInformation("Finished processing door snapshot cycle");
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogTrace("Door snapshot processing cancelled");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during door snapshot processing cycle");
                        }

                        break;
                }
            }, AwaitOperation.Drop);
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing InGameDoorsViewModel");
        _subscription.Dispose();

        _dispatcherService.PostOnUI(() =>
        {
            Doors.Clear();
            _viewModelCache.Clear();
            _logger.LogDebug("InGameDoorsViewModel collections cleared on UI thread during dispose");
        });

        _logger.LogDebug("InGameDoorsViewModel disposal complete");
    }
}

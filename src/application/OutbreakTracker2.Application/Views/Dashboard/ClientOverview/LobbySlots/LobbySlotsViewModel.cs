using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlot.Factory;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlots;

public sealed class LobbySlotsViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IDisposable _subscription;
    private readonly ILogger<LobbySlotsViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;

    // Keyed by the model's Ulid (DecodedLobbySlot.Id), not the VM's own internal Ulid
    private readonly Dictionary<Ulid, LobbySlotViewModel> _viewModelCache = [];
    private readonly ObservableList<LobbySlotViewModel> _lobbySlotsInternal = [];

    public NotifyCollectionChangedSynchronizedViewList<LobbySlotViewModel> LobbySlotsView { get; }

    public LobbySlotsViewModel(
        ITrackerRegistry trackerRegistry,
        ILogger<LobbySlotsViewModel> logger,
        IDispatcherService dispatcherService,
        ILobbySlotViewModelFactory lobbySlotVmFactory
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;

        LobbySlotsView = _lobbySlotsInternal.ToNotifyCollectionChanged(
            SynchronizationContextCollectionEventDispatcher.Current
        );

        _subscription = trackerRegistry
            .LobbySlots.Changes.Diffs.ObserveOnThreadPool()
            .SubscribeAwait(
                async (diff, cancellationToken) =>
                {
                    if (diff.Added.Count == 0 && diff.Removed.Count == 0 && diff.Changed.Count == 0)
                        return;

                    _logger.LogDebug(
                        "LobbySlot diff: +{Added} -{Removed} ~{Changed}",
                        diff.Added.Count,
                        diff.Removed.Count,
                        diff.Changed.Count
                    );

                    // Prepare new VMs on thread pool — track model Ulid alongside each VM
                    List<(Ulid SlotId, LobbySlotViewModel Vm)>? newPairs = null;
                    if (diff.Added.Count > 0)
                    {
                        newPairs = new(diff.Added.Count);
                        foreach (DecodedLobbySlot slot in diff.Added)
                            newPairs.Add((slot.Id, lobbySlotVmFactory.Create(slot)));
                    }

                    await _dispatcherService
                        .InvokeOnUIAsync(
                            () =>
                            {
                                // Individual removes
                                foreach (DecodedLobbySlot removed in diff.Removed)
                                    if (_viewModelCache.Remove(removed.Id, out LobbySlotViewModel? vm))
                                        _lobbySlotsInternal.Remove(vm);

                                // In-place updates — no list mutation
                                foreach (EntityChange<DecodedLobbySlot> change in diff.Changed)
                                    if (_viewModelCache.TryGetValue(change.Current.Id, out LobbySlotViewModel? vm))
                                        vm.Update(change.Current);

                                // Batch add — single CollectionChanged for all new slots
                                if (newPairs is { Count: > 0 })
                                {
                                    List<LobbySlotViewModel> newVms = new(newPairs.Count);
                                    foreach ((Ulid slotId, LobbySlotViewModel vm) in newPairs)
                                    {
                                        _viewModelCache[slotId] = vm;
                                        newVms.Add(vm);
                                    }

                                    _lobbySlotsInternal.AddRange(newVms);
                                }

                                _logger.LogDebug("LobbySlots updated: {Count}", _lobbySlotsInternal.Count);
                            },
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                },
                AwaitOperation.Drop
            );
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing LobbySlotsViewModel asynchronously");

        _subscription.Dispose();

        await _dispatcherService
            .InvokeOnUIAsync(() =>
            {
                _lobbySlotsInternal.Clear();
                _viewModelCache.Clear();
                LobbySlotsView.Dispose();
                _logger.LogDebug("LobbySlotsViewModel collections cleared on UI thread during async dispose");
            })
            .ConfigureAwait(false);

        _logger.LogDebug("LobbySlotsViewModel asynchronous disposal complete");
    }
}

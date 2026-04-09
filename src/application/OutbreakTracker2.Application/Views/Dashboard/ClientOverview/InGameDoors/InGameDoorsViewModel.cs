using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoor;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoors;

public sealed partial class InGameDoorsViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly ILogger<InGameDoorsViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly Dictionary<Ulid, InGameDoorViewModel> _viewModelCache = [];

    [ObservableProperty]
    private bool _hasDoors;

    [ObservableProperty]
    private bool _isHorizontalLayout;

    private ObservableList<InGameDoorViewModel> Doors { get; } = [];
    public NotifyCollectionChangedSynchronizedViewList<InGameDoorViewModel> DoorsView { get; }

    public InGameDoorsViewModel(
        ITrackerRegistry trackerRegistry,
        ILogger<InGameDoorsViewModel> logger,
        IDispatcherService dispatcherService
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;

        DoorsView = Doors.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = trackerRegistry
            .Doors.Changes.Diffs.ObserveOnThreadPool()
            .SubscribeAwait(
                async (diff, ct) =>
                {
                    if (diff.Added.Count == 0 && diff.Removed.Count == 0 && diff.Changed.Count == 0)
                        return;

                    _logger.LogDebug(
                        "Door diff: +{Added} -{Removed} ~{Changed}",
                        diff.Added.Count,
                        diff.Removed.Count,
                        diff.Changed.Count
                    );

                    // Prepare new VMs on thread pool — allocation only, no UI dependency
                    List<InGameDoorViewModel>? newVms = null;
                    if (diff.Added.Count > 0)
                    {
                        newVms = new(diff.Added.Count);
                        foreach (DecodedDoor door in diff.Added)
                            newVms.Add(new InGameDoorViewModel(door));
                    }

                    await _dispatcherService
                        .InvokeOnUIAsync(
                            () =>
                            {
                                // Individual removes — each fires one CollectionChanged
                                foreach (DecodedDoor removed in diff.Removed)
                                    if (_viewModelCache.Remove(removed.Id, out InGameDoorViewModel? vm))
                                        Doors.Remove(vm);

                                // In-place property updates — no ObservableList mutation
                                foreach (EntityChange<DecodedDoor> change in diff.Changed)
                                    if (_viewModelCache.TryGetValue(change.Current.Id, out InGameDoorViewModel? vm))
                                        vm.Update(change.Current);

                                // Batch add — single CollectionChanged for all new doors
                                if (newVms is { Count: > 0 })
                                {
                                    foreach (InGameDoorViewModel vm in newVms)
                                        _viewModelCache[vm.UniqueId] = vm;
                                    Doors.AddRange(newVms);
                                }

                                HasDoors = Doors.Count > 0;
                                _logger.LogDebug("Doors updated: {Count}", Doors.Count);
                            },
                            ct
                        )
                        .ConfigureAwait(false);
                },
                AwaitOperation.Drop
            );
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing InGameDoorsViewModel");
        _subscription.Dispose();
        DoorsView.Dispose();

        _dispatcherService.PostOnUI(() =>
        {
            Doors.Clear();
            _viewModelCache.Clear();
            _logger.LogDebug("InGameDoorsViewModel collections cleared on UI thread during dispose");
        });

        _logger.LogDebug("InGameDoorsViewModel disposal complete");
    }
}

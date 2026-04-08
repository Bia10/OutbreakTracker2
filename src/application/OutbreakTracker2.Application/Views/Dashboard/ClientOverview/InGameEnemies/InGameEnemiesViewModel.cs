using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;

public sealed partial class InGameEnemiesViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan DeadEnemyDisplayDuration = TimeSpan.FromSeconds(5);

    private readonly ILogger<InGameEnemiesViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly IDataManager _dataManager;
    private readonly IDisposable _subscription;
    private readonly Dictionary<Ulid, InGameEnemyViewModel> _viewModelCache = [];
    private readonly ObservableList<InGameEnemyViewModel> _enemies = [];
    private readonly Dictionary<Ulid, DateTimeOffset> _enemyDeathTimes = [];
    private DecodedEnemy[] _previousFilteredEnemies = [];

    public NotifyCollectionChangedSynchronizedViewList<InGameEnemyViewModel> EnemiesView { get; }

    [ObservableProperty]
    private bool _hasEnemies;

    public InGameEnemiesViewModel(
        ILogger<InGameEnemiesViewModel> logger,
        IDispatcherService dispatcherService,
        IDataManager dataManager
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _dataManager = dataManager;
        _logger.LogDebug("Initializing InGameEnemiesViewModel");

        EnemiesView = _enemies.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = dataManager
            .EnemiesObservable.ObserveOnThreadPool()
            .SubscribeAwait(
                async (snapshot, ct) =>
                {
                    if (snapshot.Length > GameConstants.MaxEnemies2)
                    {
                        _logger.LogWarning(
                            "Received {Length} enemies \u2014 exceeds in-game limit, skipping",
                            snapshot.Length
                        );
                        return;
                    }

                    // Apply stateful death-timer filter on the thread pool
                    List<DecodedEnemy> filteredList = FilterEnemiesWithDeathTimer(snapshot, DateTimeOffset.UtcNow);
                    DecodedEnemy[] filtered = [.. filteredList];

                    CollectionDiff<DecodedEnemy> diff = CollectionDiffer.Diff(_previousFilteredEnemies, filtered);
                    _previousFilteredEnemies = filtered;

                    if (diff.Added.Count == 0 && diff.Removed.Count == 0 && diff.Changed.Count == 0)
                        return;

                    _logger.LogDebug(
                        "Enemy diff: +{Added} -{Removed} ~{Changed}",
                        diff.Added.Count,
                        diff.Removed.Count,
                        diff.Changed.Count
                    );

                    // Create VMs for new entities on thread pool
                    List<InGameEnemyViewModel>? newVms = null;
                    if (diff.Added.Count > 0)
                    {
                        newVms = new(diff.Added.Count);
                        foreach (DecodedEnemy enemy in diff.Added)
                            newVms.Add(new InGameEnemyViewModel(enemy, _dataManager));
                    }

                    await _dispatcherService
                        .InvokeOnUIAsync(
                            () =>
                            {
                                // Remove stale VMs
                                foreach (DecodedEnemy removed in diff.Removed)
                                    if (_viewModelCache.Remove(removed.Id, out InGameEnemyViewModel? vm))
                                        _enemies.Remove(vm);

                                // In-place property updates — no ObservableList mutation
                                foreach (EntityChange<DecodedEnemy> change in diff.Changed)
                                    if (_viewModelCache.TryGetValue(change.Current.Id, out InGameEnemyViewModel? vm))
                                        vm.Update(change.Current);

                                // Batch add — single CollectionChanged for all new enemies
                                if (newVms is { Count: > 0 })
                                {
                                    foreach (InGameEnemyViewModel vm in newVms)
                                        _viewModelCache[vm.UniqueId] = vm;
                                    _enemies.AddRange(newVms);
                                }

                                HasEnemies = _enemies.Count > 0;
                                _logger.LogDebug("Enemies updated: {Count}", _enemies.Count);
                            },
                            ct
                        )
                        .ConfigureAwait(false);
                },
                AwaitOperation.Drop
            );
    }

    /// <summary>
    /// Determines if a DecodedEnemy passes basic validity checks (has a name, valid slot, not in spawn room).
    /// Does not filter on HP — death timer logic handles that separately.
    /// </summary>
    private static bool IsEnemyBasicallyValid(DecodedEnemy enemy) =>
        !string.IsNullOrEmpty(enemy.Name)
        && !enemy.RoomName.Equals("Spawning/Scenario Cleared", StringComparison.Ordinal)
        && enemy is { SlotId: > 0, MaxHp: > 0 };

    /// <summary>
    /// Filters the snapshot to include alive enemies and recently-dead enemies (within the 5-second grace period).
    /// Dead enemies are tracked by time-of-death so they remain visible briefly before removal.
    /// </summary>
    private List<DecodedEnemy> FilterEnemiesWithDeathTimer(DecodedEnemy[] snapshot, DateTimeOffset now)
    {
        List<DecodedEnemy> result = new(snapshot.Length);

        foreach (DecodedEnemy enemy in snapshot)
        {
            if (!IsEnemyBasicallyValid(enemy))
                continue;

            string healthStatus = InGameEnemyViewModel.GetEnemiesHealthStatusStringForFileTwo(
                enemy.SlotId,
                enemy.NameId,
                enemy.CurHp,
                enemy.MaxHp
            );
            bool isDead = InGameEnemyViewModel.IsDeadStatus(healthStatus);

            if (!isDead)
            {
                _enemyDeathTimes.Remove(enemy.Id);
                result.Add(enemy);
                continue;
            }

            // Enemy is dead: record first time of death and include within the grace period
            if (!_enemyDeathTimes.TryGetValue(enemy.Id, out DateTimeOffset deathTime))
            {
                deathTime = now;
                _enemyDeathTimes[enemy.Id] = deathTime;
                _logger.LogDebug("Enemy {UniqueId} died at {DeathTime}", enemy.Id, deathTime);
            }

            if ((now - deathTime) < DeadEnemyDisplayDuration)
                result.Add(enemy);
            else
            {
                _logger.LogDebug("Enemy {UniqueId} death grace period elapsed, removing from list", enemy.Id);
                _enemyDeathTimes.Remove(enemy.Id);
            }
        }

        // Clean up death-time entries for enemies no longer present in the snapshot
        if (_enemyDeathTimes.Count > 0)
        {
            HashSet<Ulid> snapshotIds = snapshot.Select(e => e.Id).ToHashSet();
            foreach (Ulid staleId in _enemyDeathTimes.Keys.Except(snapshotIds).ToList())
                _enemyDeathTimes.Remove(staleId);
        }

        return result;
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing InGameEnemiesViewModel");
        _subscription.Dispose();

        _dispatcherService.PostOnUI(() =>
        {
            _enemies.Clear();
            _viewModelCache.Clear();
            _enemyDeathTimes.Clear();
            _logger.LogDebug("InGameEnemiesViewModel collections cleared on UI thread during dispose");
        });

        EnemiesView.Dispose();
        _logger.LogInformation("InGameEnemiesViewModel disposed");
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;

public sealed partial class InGameEnemiesViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan DeadEnemyDisplayDuration = TimeSpan.FromSeconds(5);

    private readonly ILogger<InGameEnemiesViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly TimeProvider _timeProvider;
    private readonly IDisposable _subscription;
    private readonly Dictionary<Ulid, InGameEnemyViewModel> _viewModelCache = [];
    private readonly ObservableList<InGameEnemyViewModel> _enemies = [];
    private readonly Dictionary<Ulid, DateTimeOffset> _enemyDeathTimes = [];

    // Enemies removed from the raw stream before their death-display grace period expired.
    // Key = entity Ulid, Value = absolute expiry time.
    private readonly Dictionary<Ulid, DateTimeOffset> _pendingRemovals = [];

    public NotifyCollectionChangedSynchronizedViewList<InGameEnemyViewModel> EnemiesView { get; }

    [ObservableProperty]
    private bool _hasEnemies;

    public InGameEnemiesViewModel(
        ILogger<InGameEnemiesViewModel> logger,
        IDispatcherService dispatcherService,
        IDataManager dataManager,
        TimeProvider timeProvider,
        ITrackerRegistry trackerRegistry
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _timeProvider = timeProvider;

        EnemiesView = _enemies.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = trackerRegistry
            .Enemies.Changes.Diffs.WithLatestFrom(
                dataManager.InGameScenarioObservable,
                (diff, scenario) => (Diff: diff, ScenarioName: scenario.ScenarioName)
            )
            .ObserveOnThreadPool()
            .SubscribeAwait(
                async (data, ct) =>
                {
                    CollectionDiff<DecodedEnemy> diff = data.Diff;
                    string scenarioName = data.ScenarioName;
                    DateTimeOffset now = _timeProvider.GetUtcNow();

                    // Detect death transitions in the Changed stream, keep alive entries up to date
                    foreach (EntityChange<DecodedEnemy> change in diff.Changed)
                    {
                        string curHs = InGameEnemyViewModel.GetEnemiesHealthStatusStringForFileTwo(
                            change.Current.SlotId,
                            change.Current.NameId,
                            change.Current.CurHp,
                            change.Current.MaxHp
                        );
                        string prevHs = InGameEnemyViewModel.GetEnemiesHealthStatusStringForFileTwo(
                            change.Previous.SlotId,
                            change.Previous.NameId,
                            change.Previous.CurHp,
                            change.Previous.MaxHp
                        );

                        bool isDead = InGameEnemyViewModel.IsDeadStatus(curHs);
                        bool wasDead = InGameEnemyViewModel.IsDeadStatus(prevHs);

                        if (isDead && !wasDead)
                        {
                            _enemyDeathTimes.TryAdd(change.Current.Id, now);
                            _logger.LogDebug("Enemy {Id} died at {Time}", change.Current.Id, now);
                        }
                        else if (!isDead)
                        {
                            _enemyDeathTimes.Remove(change.Current.Id);
                        }
                    }

                    // Prepare VMs for newly-added enemies
                    List<InGameEnemyViewModel>? newVms = null;
                    if (diff.Added.Count > 0)
                    {
                        newVms = new(diff.Added.Count);
                        foreach (DecodedEnemy enemy in diff.Added)
                        {
                            if (!IsEnemyBasicallyValid(enemy))
                                continue;

                            string hs = InGameEnemyViewModel.GetEnemiesHealthStatusStringForFileTwo(
                                enemy.SlotId,
                                enemy.NameId,
                                enemy.CurHp,
                                enemy.MaxHp
                            );
                            if (InGameEnemyViewModel.IsDeadStatus(hs))
                                _enemyDeathTimes.TryAdd(enemy.Id, now);

                            newVms.Add(new InGameEnemyViewModel(enemy, scenarioName));
                        }
                    }

                    // Classify removals: immediate vs. within the grace period
                    List<Ulid>? immediateRemovals = null;
                    foreach (DecodedEnemy removed in diff.Removed)
                    {
                        if (
                            _enemyDeathTimes.TryGetValue(removed.Id, out DateTimeOffset deathTime)
                            && (now - deathTime) < DeadEnemyDisplayDuration
                        )
                        {
                            _pendingRemovals[removed.Id] = deathTime + DeadEnemyDisplayDuration;
                        }
                        else
                        {
                            immediateRemovals ??= [];
                            immediateRemovals.Add(removed.Id);
                        }

                        _enemyDeathTimes.Remove(removed.Id);
                    }

                    // Sweep pending removals whose grace period has now elapsed
                    List<Ulid>? expiredRemovals = null;
                    foreach ((Ulid id, DateTimeOffset expiryTime) in _pendingRemovals)
                    {
                        if (now >= expiryTime)
                        {
                            expiredRemovals ??= [];
                            expiredRemovals.Add(id);
                        }
                    }

                    if (expiredRemovals is not null)
                        foreach (Ulid id in expiredRemovals)
                            _pendingRemovals.Remove(id);

                    bool hasChanges =
                        immediateRemovals is not null
                        || expiredRemovals is not null
                        || (newVms is { Count: > 0 })
                        || diff.Changed.Count > 0;

                    if (!hasChanges)
                        return;

                    _logger.LogDebug(
                        "Enemy diff: +{Added} -{Removed} ~{Changed} expired:{Expired}",
                        newVms?.Count ?? 0,
                        (immediateRemovals?.Count ?? 0) + (expiredRemovals?.Count ?? 0),
                        diff.Changed.Count,
                        expiredRemovals?.Count ?? 0
                    );

                    await _dispatcherService
                        .InvokeOnUIAsync(
                            () =>
                            {
                                // Immediate removals — entity left stream with no active grace period
                                if (immediateRemovals is not null)
                                    foreach (Ulid id in immediateRemovals)
                                        if (_viewModelCache.Remove(id, out InGameEnemyViewModel? vm))
                                            _enemies.Remove(vm);

                                // Expired grace-period removals
                                if (expiredRemovals is not null)
                                    foreach (Ulid id in expiredRemovals)
                                        if (_viewModelCache.Remove(id, out InGameEnemyViewModel? vm))
                                            _enemies.Remove(vm);

                                // In-place property updates — no ObservableList mutation
                                foreach (EntityChange<DecodedEnemy> change in diff.Changed)
                                    if (_viewModelCache.TryGetValue(change.Current.Id, out InGameEnemyViewModel? vm))
                                        vm.Update(change.Current, scenarioName);

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
    /// Determines if a <see cref="DecodedEnemy"/> passes basic validity checks (has a name,
    /// valid slot, not in spawn room). Death-timer logic is handled separately.
    /// </summary>
    private static bool IsEnemyBasicallyValid(DecodedEnemy enemy) =>
        !string.IsNullOrEmpty(enemy.Name)
        && !enemy.RoomName.Equals("Spawning/Scenario Cleared", StringComparison.Ordinal)
        && enemy is { SlotId: > 0, MaxHp: > 0 };

    public void Dispose()
    {
        _logger.LogDebug("Disposing InGameEnemiesViewModel");
        _subscription.Dispose();

        _dispatcherService.PostOnUI(() =>
        {
            _enemies.Clear();
            _viewModelCache.Clear();
            _enemyDeathTimes.Clear();
            _pendingRemovals.Clear();
            EnemiesView.Dispose();
            _logger.LogDebug("InGameEnemiesViewModel collections cleared on UI thread during dispose");
        });

        _logger.LogDebug("InGameEnemiesViewModel disposed");
    }
}

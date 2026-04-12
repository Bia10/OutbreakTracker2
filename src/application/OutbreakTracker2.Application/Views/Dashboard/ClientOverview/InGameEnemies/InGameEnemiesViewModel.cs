using System.Collections.Specialized;
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

public sealed partial class InGameEnemiesViewModel : ObservableObject, IDisposable, IEnemyCardCollectionSource
{
    private readonly ILogger<InGameEnemiesViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly TimeProvider _timeProvider;
    private readonly IDisposable _subscription;
    private readonly EnemyDiffPlanner _enemyDiffPlanner = new();
    private readonly Dictionary<Ulid, InGameEnemyViewModel> _viewModelCache = [];
    private readonly ObservableList<InGameEnemyViewModel> _enemies = [];

    public NotifyCollectionChangedSynchronizedViewList<InGameEnemyViewModel> EnemiesView { get; }

    [ObservableProperty]
    private bool _hasEnemies;

    public InGameEnemiesViewModel(
        ILogger<InGameEnemiesViewModel> logger,
        IDispatcherService dispatcherService,
        IDataObservableSource dataObservable,
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
                dataObservable.InGameScenarioObservable,
                (diff, scenario) => (Diff: diff, ScenarioName: scenario.ScenarioName)
            )
            .SubscribeAwait(
                async (data, ct) =>
                {
                    CollectionDiff<DecodedEnemy> diff = data.Diff;
                    string scenarioName = data.ScenarioName;
                    DateTimeOffset now = _timeProvider.GetUtcNow();

                    EnemyListUpdatePlan plan = _enemyDiffPlanner.BuildPlan(diff, scenarioName, now);
                    if (!plan.HasChanges)
                        return;

                    _logger.LogDebug(
                        "Enemy diff plan: +{Added} -{Removed} ~{Changed}",
                        plan.NewViewModels.Count,
                        plan.RemovedIds.Count,
                        plan.UpdatedEnemies.Count
                    );

                    await _dispatcherService
                        .InvokeOnUIAsync(
                            () =>
                            {
                                EnemyListReconciler.Apply(plan, _enemies, _viewModelCache);

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

    IEnumerable<InGameEnemyViewModel> IEnemyCardCollectionSource.Enemies => EnemiesView;

    event NotifyCollectionChangedEventHandler? IEnemyCardCollectionSource.CollectionChanged
    {
        add => ((INotifyCollectionChanged)EnemiesView).CollectionChanged += value;
        remove => ((INotifyCollectionChanged)EnemiesView).CollectionChanged -= value;
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing InGameEnemiesViewModel");
        _subscription.Dispose();
        _enemyDiffPlanner.Clear();

        _dispatcherService.PostOnUI(() =>
        {
            _enemies.Clear();
            _viewModelCache.Clear();
            EnemiesView.Dispose();
            _logger.LogDebug("InGameEnemiesViewModel collections cleared on UI thread during dispose");
        });

        _logger.LogDebug("InGameEnemiesViewModel disposed");
    }
}

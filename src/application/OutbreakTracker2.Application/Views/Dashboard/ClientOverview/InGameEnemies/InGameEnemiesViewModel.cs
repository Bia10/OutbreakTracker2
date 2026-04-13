using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Utilities;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;

public sealed partial class InGameEnemiesViewModel
    : ObservableObject,
        IDisposable,
        IAsyncDisposable,
        IEnemyCardCollectionSource
{
    private readonly ILogger<InGameEnemiesViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly TimeProvider _timeProvider;
    private readonly IDisposable _subscription;
    private readonly IDisposable _scenarioStatusSubscription;
    private readonly IDisposable _settingsSubscription;
    private readonly EnemyDiffPlanner _enemyDiffPlanner = new();
    private readonly Dictionary<Ulid, InGameEnemyViewModel> _viewModelCache = [];
    private readonly ObservableList<InGameEnemyViewModel> _enemies = [];
    private int _disposeState;
    private ScenarioStatus _scenarioStatus;
    private bool _showGameplayUiDuringTransitions;

    public NotifyCollectionChangedSynchronizedViewList<InGameEnemyViewModel> EnemiesView { get; }

    [ObservableProperty]
    private bool _hasEnemies;

    public InGameEnemiesViewModel(
        ILogger<InGameEnemiesViewModel> logger,
        IDispatcherService dispatcherService,
        IDataObservableSource dataObservable,
        TimeProvider timeProvider,
        IAppSettingsService settingsService,
        ITrackerRegistry trackerRegistry
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _timeProvider = timeProvider;
        _showGameplayUiDuringTransitions = GetDisplaySettings(settingsService.Current).ShowGameplayUiDuringTransitions;

        EnemiesView = _enemies.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = trackerRegistry
            .EnemyChanges.Diffs.WithLatestFrom(
                dataObservable.InGameScenarioObservable,
                (diff, scenario) => (Diff: diff, ScenarioName: scenario.ScenarioName, ScenarioStatus: scenario.Status)
            )
            .SubscribeAwait(
                async (data, ct) =>
                {
                    CollectionDiff<DecodedEnemy> diff = data.Diff;
                    string scenarioName = data.ScenarioName;
                    ScenarioStatus scenarioStatus = data.ScenarioStatus;
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
                                ApplyEnemyPlan(plan);
                                _scenarioStatus = scenarioStatus;

                                HasEnemies =
                                    _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions)
                                    && _enemies.Count > 0;
                                _logger.LogDebug("Enemies updated: {Count}", _enemies.Count);
                            },
                            ct
                        )
                        .ConfigureAwait(false);
                },
                AwaitOperation.Drop
            );

        _scenarioStatusSubscription = dataObservable
            .InGameScenarioObservable.Select(static scenario => scenario.Status)
            .DistinctUntilChanged()
            .Subscribe(status => _dispatcherService.PostOnUI(() => UpdateVisibleState(status)));

        _settingsSubscription = settingsService
            .SettingsObservable.Select(static settings => GetDisplaySettings(settings).ShowGameplayUiDuringTransitions)
            .DistinctUntilChanged()
            .Subscribe(show => _dispatcherService.PostOnUI(() => UpdateTransitionDisplaySetting(show)));
    }

    IEnumerable<InGameEnemyViewModel> IEnemyCardCollectionSource.Enemies => EnemiesView;

    event NotifyCollectionChangedEventHandler? IEnemyCardCollectionSource.CollectionChanged
    {
        add => ((INotifyCollectionChanged)EnemiesView).CollectionChanged += value;
        remove => ((INotifyCollectionChanged)EnemiesView).CollectionChanged -= value;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        _logger.LogDebug("Disposing InGameEnemiesViewModel");
        DisposeSubscriptions();

        if (_dispatcherService.IsOnUIThread())
            DisposeCollectionsOnUiThread();
        else
            _dispatcherService.InvokeOnUIAsync(DisposeCollectionsOnUiThread).GetAwaiter().GetResult();

        GC.SuppressFinalize(this);
        _logger.LogDebug("InGameEnemiesViewModel synchronous disposal complete");
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        _logger.LogDebug("Disposing InGameEnemiesViewModel asynchronously");
        DisposeSubscriptions();

        if (_dispatcherService.IsOnUIThread())
            DisposeCollectionsOnUiThread();
        else
            await _dispatcherService.InvokeOnUIAsync(DisposeCollectionsOnUiThread).ConfigureAwait(false);

        GC.SuppressFinalize(this);
        _logger.LogDebug("InGameEnemiesViewModel asynchronous disposal complete");
    }

    private void DisposeSubscriptions()
    {
        _subscription.Dispose();
        _scenarioStatusSubscription.Dispose();
        _settingsSubscription.Dispose();
        _enemyDiffPlanner.Clear();
    }

    private void DisposeCollectionsOnUiThread()
    {
        _enemies.Clear();
        _viewModelCache.Clear();
        HasEnemies = false;
        EnemiesView.Dispose();
        _logger.LogDebug("InGameEnemiesViewModel collections cleared on UI thread during dispose");
    }

    private void ApplyEnemyPlan(EnemyListUpdatePlan plan)
    {
        HashSet<Ulid> removedIds = [.. plan.RemovedIds];

        foreach (Ulid removedId in removedIds)
            _viewModelCache.Remove(removedId);

        foreach (EntityChange<DecodedEnemy> change in plan.UpdatedEnemies)
        {
            if (_viewModelCache.TryGetValue(change.Current.Id, out InGameEnemyViewModel? viewModel))
                viewModel.Update(change.Current, plan.ScenarioName);
        }

        foreach (InGameEnemyViewModel viewModel in plan.NewViewModels)
            _viewModelCache[viewModel.UniqueId] = viewModel;

        OrderedObservableListReconciler.ApplyViewModels(
            _enemies,
            BuildDesiredEnemyViewModels(plan.NewViewModels, removedIds),
            static viewModel => viewModel.UniqueId
        );
    }

    private List<InGameEnemyViewModel> BuildDesiredEnemyViewModels(
        IReadOnlyList<InGameEnemyViewModel> newViewModels,
        IReadOnlySet<Ulid> removedIds
    )
    {
        List<InGameEnemyViewModel> desiredViewModels = new(_enemies.Count + newViewModels.Count);
        HashSet<Ulid> seenIds = [];

        foreach (InGameEnemyViewModel viewModel in _enemies)
        {
            if (removedIds.Contains(viewModel.UniqueId) || !seenIds.Add(viewModel.UniqueId))
                continue;

            if (_viewModelCache.TryGetValue(viewModel.UniqueId, out InGameEnemyViewModel? cachedViewModel))
                desiredViewModels.Add(cachedViewModel);
        }

        foreach (InGameEnemyViewModel viewModel in newViewModels)
        {
            if (!seenIds.Add(viewModel.UniqueId))
                continue;

            desiredViewModels.Add(viewModel);
        }

        return desiredViewModels;
    }

    private void UpdateVisibleState(ScenarioStatus scenarioStatus)
    {
        _scenarioStatus = scenarioStatus;
        HasEnemies = _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions) && _enemies.Count > 0;
    }

    private void UpdateTransitionDisplaySetting(bool showGameplayUiDuringTransitions)
    {
        if (_showGameplayUiDuringTransitions == showGameplayUiDuringTransitions)
            return;

        _showGameplayUiDuringTransitions = showGameplayUiDuringTransitions;
        HasEnemies = _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions) && _enemies.Count > 0;
    }

    private static DisplaySettings GetDisplaySettings(OutbreakTrackerSettings settings) =>
        settings.Display ?? new DisplaySettings();
}

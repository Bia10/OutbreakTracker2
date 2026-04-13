using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;

public sealed partial class ScenarioEnemiesViewModel : ObservableObject, IDisposable, IAsyncDisposable
{
    private readonly ILogger<ScenarioEnemiesViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly ObservableList<DecodedEnemy> _enemies = [];
    private int _disposeState;
    private ScenarioStatus _scenarioStatus;
    private bool _showGameplayUiDuringTransitions;
    private DisposableBag _disposables;

    public NotifyCollectionChangedSynchronizedViewList<DecodedEnemy> Enemies { get; }

    [ObservableProperty]
    private bool _hasEnemies;

    public ScenarioEnemiesViewModel(
        ILogger<ScenarioEnemiesViewModel> logger,
        IDataObservableSource dataObservable,
        IAppSettingsService settingsService,
        IDispatcherService dispatcherService
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _showGameplayUiDuringTransitions = GetDisplaySettings(settingsService.Current).ShowGameplayUiDuringTransitions;
        Enemies = _enemies.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _disposables.Add(
            dataObservable
                .InGameOverviewObservable.ObserveOnThreadPool()
                .SubscribeAwait(
                    async (snapshot, cancellationToken) =>
                    {
                        try
                        {
                            await dispatcherService
                                .InvokeOnUIAsync(() => ApplySnapshot(snapshot), cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Scenario enemy update processing cancelled");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during scenario enemy update processing cycle");
                        }
                    },
                    AwaitOperation.Drop
                )
        );

        _disposables.Add(
            settingsService
                .SettingsObservable.Select(static settings =>
                    GetDisplaySettings(settings).ShowGameplayUiDuringTransitions
                )
                .DistinctUntilChanged()
                .Subscribe(show => dispatcherService.PostOnUI(() => UpdateTransitionDisplaySetting(show)))
        );
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        _disposables.Dispose();

        if (_dispatcherService.IsOnUIThread())
            DisposeCollectionsOnUiThread();
        else
            _dispatcherService.InvokeOnUIAsync(DisposeCollectionsOnUiThread).GetAwaiter().GetResult();

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        _disposables.Dispose();

        if (_dispatcherService.IsOnUIThread())
            DisposeCollectionsOnUiThread();
        else
            await _dispatcherService.InvokeOnUIAsync(DisposeCollectionsOnUiThread).ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    public void ClearEnemies()
    {
        _enemies.Clear();
        HasEnemies = false;
    }

    public void UpdateEnemies(DecodedEnemy[] newEnemies)
    {
        ArgumentNullException.ThrowIfNull(newEnemies);

        // Batch reset: Clear fires a single Reset event, AddRange fires a single Add event.
        // This avoids individual Remove events which trigger DataGrid virtualization
        // RemoveNonDisplayedRows during a measure pass, causing an ArgumentOutOfRangeException.
        _enemies.Clear();
        if (newEnemies.Length > 0)
            _enemies.AddRange(newEnemies);

        HasEnemies = _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions) && _enemies.Count > 0;
    }

    private void ApplySnapshot(InGameOverviewSnapshot snapshot)
    {
        _scenarioStatus = snapshot.Scenario.Status;

        if (snapshot.Scenario.CurrentFile is < 1 or > 2)
        {
            ClearEnemies();
            return;
        }

        UpdateEnemies(snapshot.Enemies);
    }

    private void UpdateTransitionDisplaySetting(bool showGameplayUiDuringTransitions)
    {
        if (_showGameplayUiDuringTransitions == showGameplayUiDuringTransitions)
            return;

        _showGameplayUiDuringTransitions = showGameplayUiDuringTransitions;
        HasEnemies = _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions) && _enemies.Count > 0;
    }

    private void DisposeCollectionsOnUiThread()
    {
        ClearEnemies();
        Enemies.Dispose();
    }

    private static DisplaySettings GetDisplaySettings(OutbreakTrackerSettings settings) =>
        settings.Display ?? new DisplaySettings();
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.Factory;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayers;

public sealed partial class InGamePlayersViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IDisposable _subscription;
    private readonly IDisposable _scenarioStatusSubscription;
    private readonly IDisposable _settingsSubscription;
    private readonly ILogger<InGamePlayersViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly IInGamePlayerViewModelFactory _inGamePlayerViewModelFactory;
    private readonly Dictionary<Ulid, InGamePlayerViewModel> _viewModelCache = [];
    private ScenarioStatus _scenarioStatus;
    private bool _showGameplayUiDuringTransitions;

    private readonly ObservableList<InGamePlayerViewModel> _players = [];
    public NotifyCollectionChangedSynchronizedViewList<InGamePlayerViewModel> PlayersView { get; }

    [ObservableProperty]
    private bool _hasPlayers;

    [ObservableProperty]
    private int _playerColumnCount = 1;

    [ObservableProperty]
    private bool _isHorizontalLayout = true;

    public InGamePlayersViewModel(
        IDataObservableSource dataObservable,
        ITrackerRegistry trackerRegistry,
        IAppSettingsService settingsService,
        ILogger<InGamePlayersViewModel> logger,
        IDispatcherService dispatcherService,
        IInGamePlayerViewModelFactory inGamePlayerViewModelFactory
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _inGamePlayerViewModelFactory = inGamePlayerViewModelFactory;
        _showGameplayUiDuringTransitions = GetDisplaySettings(settingsService.Current).ShowGameplayUiDuringTransitions;

        PlayersView = _players.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = trackerRegistry
            .Players.Changes.Diffs.WithLatestFrom(
                dataObservable.InGameScenarioObservable,
                (diff, scenario) =>
                    (
                        Diff: diff,
                        ScenarioStatus: scenario.Status,
                        ScenarioName: scenario.ScenarioName,
                        CurrentFile: scenario.CurrentFile,
                        ScenarioItems: scenario.Items
                    )
            )
            .ObserveOnThreadPool()
            .SubscribeAwait(
                async (data, ct) =>
                {
                    CollectionDiff<DecodedInGamePlayer> diff = data.Diff;
                    ScenarioStatus lastScenarioStatus = data.ScenarioStatus;
                    string scenarioName = data.ScenarioName;
                    byte currentGameFile = data.CurrentFile;
                    DecodedItem[] scenarioItems = data.ScenarioItems;

                    _logger.LogTrace(
                        "Processing player diff on thread pool: +{Added} -{Removed} ~{Changed}",
                        diff.Added.Count,
                        diff.Removed.Count,
                        diff.Changed.Count
                    );

                    try
                    {
                        // New active players — create VMs on the thread pool
                        List<(Ulid Id, InGamePlayerViewModel Vm)>? newVms = null;
                        if (diff.Added.Count > 0)
                        {
                            foreach (DecodedInGamePlayer player in diff.Added)
                            {
                                if (!IsPlayerActive(player))
                                    continue;

                                ct.ThrowIfCancellationRequested();
                                _logger.LogDebug("Creating new ViewModel for player slot {Id}", player.Id);
                                InGamePlayerViewModel vm = _inGamePlayerViewModelFactory.Create(
                                    player,
                                    currentGameFile,
                                    scenarioName,
                                    scenarioItems
                                );
                                newVms ??= [];
                                newVms.Add((player.Id, vm));
                            }
                        }

                        // In Changed, detect players that became active (late-join) or went inactive
                        List<(Ulid Id, InGamePlayerViewModel Vm)>? lateJoins = null;
                        List<Ulid>? becameInactive = null;
                        foreach (EntityChange<DecodedInGamePlayer> change in diff.Changed)
                        {
                            ct.ThrowIfCancellationRequested();
                            bool wasActive = IsPlayerActive(change.Previous);
                            bool isActive = IsPlayerActive(change.Current);

                            if (!wasActive && isActive)
                            {
                                _logger.LogDebug("Player {Id} became active; creating VM", change.Current.Id);
                                InGamePlayerViewModel vm = _inGamePlayerViewModelFactory.Create(
                                    change.Current,
                                    currentGameFile,
                                    scenarioName,
                                    scenarioItems
                                );
                                lateJoins ??= [];
                                lateJoins.Add((change.Current.Id, vm));
                            }
                            else if (wasActive && !isActive)
                            {
                                becameInactive ??= [];
                                becameInactive.Add(change.Current.Id);
                            }
                        }

                        await _dispatcherService
                            .InvokeOnUIAsync(
                                () =>
                                {
                                    _logger.LogTrace("Applying player updates on UI thread");
                                    _scenarioStatus = lastScenarioStatus;

                                    // Removals from raw stream — honour transitional-status guard
                                    if (!lastScenarioStatus.IsTransitional())
                                        foreach (DecodedInGamePlayer removed in diff.Removed)
                                            RemovePlayer(removed.Id);

                                    // Players that went inactive mid-session
                                    if (becameInactive is not null)
                                        foreach (Ulid id in becameInactive)
                                            RemovePlayer(id);

                                    // In-place updates for currently-tracked active players
                                    foreach (EntityChange<DecodedInGamePlayer> change in diff.Changed)
                                        if (
                                            IsPlayerActive(change.Current)
                                            && _viewModelCache.TryGetValue(
                                                change.Current.Id,
                                                out InGamePlayerViewModel? vm
                                            )
                                        )
                                            vm.Update(change.Current, currentGameFile, scenarioName, scenarioItems);

                                    // Add newly-appeared players
                                    if (newVms is not null)
                                        foreach ((Ulid id, InGamePlayerViewModel vm) in newVms)
                                            AddPlayer(id, vm);

                                    // Late-join players that became active after the tracker saw them
                                    if (lateJoins is not null)
                                        foreach ((Ulid id, InGamePlayerViewModel vm) in lateJoins)
                                            AddPlayer(id, vm);

                                    HasPlayers =
                                        _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions)
                                        && _players.Count > 0;
                                    PlayerColumnCount = _players.Count > 0 ? _players.Count : 1;

                                    _logger.LogTrace("UI update complete. Players count: {Count}", _players.Count);
                                },
                                ct
                            )
                            .ConfigureAwait(false);

                        _logger.LogTrace("Finished processing player diff cycle");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogTrace("Player diff processing cancelled");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during player diff processing cycle");
                    }
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

    private void AddPlayer(Ulid id, InGamePlayerViewModel vm)
    {
        if (_viewModelCache.TryAdd(id, vm))
        {
            _players.Add(vm);
            _logger.LogDebug("Added player VM {Id}", id);
        }
    }

    private void RemovePlayer(Ulid id)
    {
        if (_viewModelCache.Remove(id, out InGamePlayerViewModel? vm))
        {
            _players.Remove(vm);
            vm.Dispose();
            _logger.LogDebug("Removed player VM {Id}", id);
        }
    }

    private static bool IsPlayerActive(DecodedInGamePlayer? player)
    {
        if (player is null || !player.IsEnabled)
            return false;

        return player.NameId > 0 || !string.IsNullOrEmpty(player.Type);
    }

    private void UpdateVisibleState(ScenarioStatus scenarioStatus)
    {
        _scenarioStatus = scenarioStatus;
        HasPlayers = _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions) && _players.Count > 0;
        PlayerColumnCount = _players.Count > 0 ? _players.Count : 1;
    }

    private void UpdateTransitionDisplaySetting(bool showGameplayUiDuringTransitions)
    {
        if (_showGameplayUiDuringTransitions == showGameplayUiDuringTransitions)
            return;

        _showGameplayUiDuringTransitions = showGameplayUiDuringTransitions;
        HasPlayers = _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions) && _players.Count > 0;
    }

    private static DisplaySettings GetDisplaySettings(OutbreakTrackerSettings settings) =>
        settings.Display ?? new DisplaySettings();

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing InGamePlayersViewModel");
        _subscription.Dispose();
        _scenarioStatusSubscription.Dispose();
        _settingsSubscription.Dispose();

        await _dispatcherService
            .InvokeOnUIAsync(() =>
            {
                foreach (InGamePlayerViewModel player in _viewModelCache.Values)
                    player.Dispose();

                _players.Clear();
                _viewModelCache.Clear();
                PlayersView.Dispose();
                _logger.LogDebug("InGamePlayersViewModel collections cleared on UI thread during async dispose");
            })
            .ConfigureAwait(false);

        _logger.LogDebug("InGamePlayersViewModel asynchronous disposal complete");
    }
}

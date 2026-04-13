using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Extensions;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

public sealed partial class EntitiesDockViewModel : ObservableObject, IDisposable
{
    private const string DefaultEmptyMessage = "No active enemies - not in-game";
    private const string CurrentRoomEmptyMessage = "No entities in your room";

    private readonly ILogger<EntitiesDockViewModel> _logger;
    private readonly IEnemyCardCollectionSource _source;
    private readonly IDispatcherService _dispatcherService;
    private readonly ObservableList<InGameEnemyViewModel> _enemies = [];
    private readonly HashSet<InGameEnemyViewModel> _observedEnemies = [];
    private DisposableBag _disposables;
    private bool _onlyShowCurrentPlayerRoom;
    private bool _hasCurrentPlayerRoom;
    private short _currentPlayerRoomId;
    private ScenarioStatus _scenarioStatus;
    private bool _showGameplayUiDuringTransitions;

    public NotifyCollectionChangedSynchronizedViewList<InGameEnemyViewModel> EnemiesView { get; }

    [ObservableProperty]
    private bool _hasEnemies;

    [ObservableProperty]
    private string _emptyMessage = DefaultEmptyMessage;

    public EntitiesDockViewModel(
        IEnemyCardCollectionSource source,
        IDataObservableSource dataObservable,
        IDataSnapshot dataSnapshot,
        IAppSettingsService settingsService,
        IDispatcherService dispatcherService,
        ILogger<EntitiesDockViewModel> logger
    )
    {
        _logger = logger;
        _source = source;
        _dispatcherService = dispatcherService;

        EnemiesView = _enemies.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _onlyShowCurrentPlayerRoom = GetEntitiesDockSettings(settingsService.Current).OnlyShowCurrentPlayerRoom;
        _showGameplayUiDuringTransitions = GetDisplaySettings(settingsService.Current).ShowGameplayUiDuringTransitions;
        _scenarioStatus = dataSnapshot.InGameScenario.Status;

        (bool hasRoom, short roomId) currentPlayerRoom = ResolveCurrentPlayerRoom(
            dataSnapshot.InGamePlayers,
            dataSnapshot.InGameScenario.LocalPlayerSlotIndex
        );
        _hasCurrentPlayerRoom = currentPlayerRoom.hasRoom;
        _currentPlayerRoomId = currentPlayerRoom.roomId;

        ResetObservedEnemies();
        _source.CollectionChanged += OnSourceCollectionChanged;

        _disposables.Add(
            dataObservable
                .InGamePlayersObservable.WithLatestFrom(
                    dataObservable.InGameScenarioObservable,
                    static (players, scenario) => ResolveCurrentPlayerRoom(players, scenario.LocalPlayerSlotIndex)
                )
                .Subscribe(
                    onNext: roomState => _dispatcherService.PostOnUI(() => UpdateCurrentPlayerRoom(roomState)),
                    onErrorResume: ex =>
                        _logger.LogError(ex, "Error while monitoring entities-dock player room updates"),
                    onCompleted: _ => _logger.LogInformation("Entities-dock player-room stream completed")
                )
        );

        _disposables.Add(
            dataObservable
                .InGameScenarioObservable.WithLatestFrom(
                    dataObservable.InGamePlayersObservable,
                    static (scenario, players) => ResolveCurrentPlayerRoom(players, scenario.LocalPlayerSlotIndex)
                )
                .Subscribe(
                    onNext: roomState => _dispatcherService.PostOnUI(() => UpdateCurrentPlayerRoom(roomState)),
                    onErrorResume: ex =>
                        _logger.LogError(ex, "Error while monitoring entities-dock scenario room updates"),
                    onCompleted: _ => _logger.LogInformation("Entities-dock scenario-room stream completed")
                )
        );

        _disposables.Add(
            settingsService
                .SettingsObservable.Select(static settings =>
                    (
                        OnlyShowCurrentPlayerRoom: GetEntitiesDockSettings(settings).OnlyShowCurrentPlayerRoom,
                        ShowGameplayUiDuringTransitions: GetDisplaySettings(settings).ShowGameplayUiDuringTransitions
                    )
                )
                .Subscribe(
                    onNext: state =>
                        _dispatcherService.PostOnUI(() =>
                            UpdateDisplaySettings(
                                state.OnlyShowCurrentPlayerRoom,
                                state.ShowGameplayUiDuringTransitions
                            )
                        ),
                    onErrorResume: ex => _logger.LogError(ex, "Error while monitoring entities-dock settings"),
                    onCompleted: _ => _logger.LogInformation("Entities-dock settings stream completed")
                )
        );

        _disposables.Add(
            dataObservable
                .InGameScenarioObservable.Select(static scenario => scenario.Status)
                .DistinctUntilChanged()
                .Subscribe(
                    onNext: status => _dispatcherService.PostOnUI(() => UpdateScenarioStatus(status)),
                    onErrorResume: ex => _logger.LogError(ex, "Error while monitoring entities-dock scenario status"),
                    onCompleted: _ => _logger.LogInformation("Entities-dock scenario-status stream completed")
                )
        );

        // Defer the initial rebuild so the dock panel can appear immediately.
        // ResetObservedEnemies() wires PropertyChanged handlers synchronously and is fast;
        // RebuildFilteredEnemies() triggers CollectionChanged → Avalonia item realization
        // which can take 1-2 s for large enemy pools and must not block the constructor.
        _dispatcherService.PostOnUI(RebuildFilteredEnemies);
    }

    public void Dispose()
    {
        _source.CollectionChanged -= OnSourceCollectionChanged;

        foreach (InGameEnemyViewModel enemy in _observedEnemies)
            enemy.PropertyChanged -= OnEnemyPropertyChanged;

        _observedEnemies.Clear();
        _disposables.Dispose();
        EnemiesView.Dispose();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            ResetObservedEnemies();
            RebuildFilteredEnemies();
            return;
        }

        if (e.OldItems is not null)
            foreach (object? item in e.OldItems)
                if (item is InGameEnemyViewModel enemy)
                    DetachEnemy(enemy);

        if (e.NewItems is not null)
            foreach (object? item in e.NewItems)
                if (item is InGameEnemyViewModel enemy)
                    AttachEnemy(enemy);

        RebuildFilteredEnemies();
    }

    private void OnEnemyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            !string.IsNullOrEmpty(e.PropertyName)
            && !string.Equals(e.PropertyName, nameof(InGameEnemyViewModel.RoomId), StringComparison.Ordinal)
        )
            return;

        RebuildFilteredEnemies();
    }

    private void ResetObservedEnemies()
    {
        foreach (InGameEnemyViewModel enemy in _observedEnemies)
            enemy.PropertyChanged -= OnEnemyPropertyChanged;

        _observedEnemies.Clear();

        foreach (InGameEnemyViewModel enemy in _source.Enemies)
            AttachEnemy(enemy);
    }

    private void AttachEnemy(InGameEnemyViewModel enemy)
    {
        if (_observedEnemies.Add(enemy))
            enemy.PropertyChanged += OnEnemyPropertyChanged;
    }

    private void DetachEnemy(InGameEnemyViewModel enemy)
    {
        if (_observedEnemies.Remove(enemy))
            enemy.PropertyChanged -= OnEnemyPropertyChanged;
    }

    private void UpdateDisplaySettings(bool onlyShowCurrentPlayerRoom, bool showGameplayUiDuringTransitions)
    {
        if (
            _onlyShowCurrentPlayerRoom == onlyShowCurrentPlayerRoom
            && _showGameplayUiDuringTransitions == showGameplayUiDuringTransitions
        )
            return;

        _onlyShowCurrentPlayerRoom = onlyShowCurrentPlayerRoom;
        _showGameplayUiDuringTransitions = showGameplayUiDuringTransitions;
        RebuildFilteredEnemies();
    }

    private void UpdateCurrentPlayerRoom((bool HasRoom, short RoomId) roomState)
    {
        if (
            _hasCurrentPlayerRoom == roomState.HasRoom
            && (!_hasCurrentPlayerRoom || _currentPlayerRoomId == roomState.RoomId)
        )
            return;

        _hasCurrentPlayerRoom = roomState.HasRoom;
        _currentPlayerRoomId = roomState.RoomId;
        RebuildFilteredEnemies();
    }

    private void UpdateScenarioStatus(ScenarioStatus scenarioStatus)
    {
        if (_scenarioStatus == scenarioStatus)
            return;

        _scenarioStatus = scenarioStatus;
        RebuildFilteredEnemies();
    }

    private void RebuildFilteredEnemies()
    {
        List<InGameEnemyViewModel> filteredEnemies = [];

        foreach (InGameEnemyViewModel enemy in _source.Enemies)
        {
            if (ShouldInclude(enemy))
                filteredEnemies.Add(enemy);
        }

        bool isGameplayActive = _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions);
        bool hasEnemies = isGameplayActive && filteredEnemies.Count > 0;
        string emptyMessage =
            hasEnemies ? string.Empty
            : isGameplayActive && _onlyShowCurrentPlayerRoom && _hasCurrentPlayerRoom ? CurrentRoomEmptyMessage
            : DefaultEmptyMessage;

        if (!HasSameEnemySequence(filteredEnemies))
            _enemies.ReplaceAll(filteredEnemies);

        HasEnemies = hasEnemies;
        EmptyMessage = emptyMessage;
    }

    private bool ShouldInclude(InGameEnemyViewModel enemy)
    {
        if (!_onlyShowCurrentPlayerRoom || !_hasCurrentPlayerRoom)
            return true;

        return enemy.RoomId == _currentPlayerRoomId;
    }

    private bool HasSameEnemySequence(List<InGameEnemyViewModel> filteredEnemies)
    {
        if (_enemies.Count != filteredEnemies.Count)
            return false;

        int index = 0;
        foreach (InGameEnemyViewModel enemy in _enemies)
        {
            if (!ReferenceEquals(enemy, filteredEnemies[index]))
                return false;

            index++;
        }

        return true;
    }

    private static EntitiesDockSettings GetEntitiesDockSettings(OutbreakTrackerSettings settings) =>
        (settings.Display ?? new DisplaySettings()).EntitiesDock ?? new EntitiesDockSettings();

    private static DisplaySettings GetDisplaySettings(OutbreakTrackerSettings settings) =>
        settings.Display ?? new DisplaySettings();

    private static (bool HasRoom, short RoomId) ResolveCurrentPlayerRoom(
        DecodedInGamePlayer[] players,
        byte localPlayerSlotIndex
    ) => CurrentPlayerRoomResolver.Resolve(players, localPlayerSlotIndex);
}

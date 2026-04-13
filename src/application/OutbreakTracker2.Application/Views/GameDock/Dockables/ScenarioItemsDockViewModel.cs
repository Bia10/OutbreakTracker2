using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Outbreak.Enums;
using R3;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

public sealed partial class ScenarioItemsDockViewModel : ObservableObject, IDisposable
{
    private const string DefaultEmptyMessage = "No items - not in-game";
    private const string CurrentRoomEmptyMessage = "No items in your room";

    private readonly ScenarioItemsViewModel _source;
    private readonly ObservableList<ScenarioRoomGroupViewModel> _roomGroups = [];
    private DisposableBag _disposables;
    private bool _onlyShowCurrentPlayerRoom;
    private bool _hasCurrentPlayerRoom;
    private short _currentPlayerRoomId;
    private ScenarioStatus _scenarioStatus;
    private bool _showGameplayUiDuringTransitions;

    public NotifyCollectionChangedSynchronizedViewList<ScenarioRoomGroupViewModel> RoomGroups { get; }

    [ObservableProperty]
    private bool _hasRoomGroups;

    [ObservableProperty]
    private string _emptyMessage = DefaultEmptyMessage;

    public ScenarioItemsDockViewModel(
        ScenarioItemsViewModel source,
        IDataObservableSource dataObservable,
        IDataSnapshot dataSnapshot,
        IAppSettingsService settingsService,
        IDispatcherService dispatcherService
    )
    {
        _source = source;

        RoomGroups = _roomGroups.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _onlyShowCurrentPlayerRoom = GetScenarioItemsDockSettings(settingsService.Current).OnlyShowCurrentPlayerRoom;
        _showGameplayUiDuringTransitions = GetDisplaySettings(settingsService.Current).ShowGameplayUiDuringTransitions;
        _scenarioStatus = dataSnapshot.InGameScenario.Status;

        (bool hasRoom, short roomId) currentPlayerRoom = CurrentPlayerRoomResolver.Resolve(
            dataSnapshot.InGamePlayers,
            dataSnapshot.InGameScenario.LocalPlayerSlotIndex
        );
        _hasCurrentPlayerRoom = currentPlayerRoom.hasRoom;
        _currentPlayerRoomId = currentPlayerRoom.roomId;

        ((INotifyCollectionChanged)_source.RoomGroups).CollectionChanged += OnSourceRoomGroupsChanged;

        _disposables.Add(
            dataObservable
                .InGamePlayersObservable.WithLatestFrom(
                    dataObservable.InGameScenarioObservable,
                    static (players, scenario) =>
                        CurrentPlayerRoomResolver.Resolve(players, scenario.LocalPlayerSlotIndex)
                )
                .Subscribe(roomState => dispatcherService.PostOnUI(() => UpdateCurrentPlayerRoom(roomState)))
        );

        _disposables.Add(
            dataObservable
                .InGameScenarioObservable.WithLatestFrom(
                    dataObservable.InGamePlayersObservable,
                    static (scenario, players) =>
                        CurrentPlayerRoomResolver.Resolve(players, scenario.LocalPlayerSlotIndex)
                )
                .Subscribe(roomState => dispatcherService.PostOnUI(() => UpdateCurrentPlayerRoom(roomState)))
        );

        _disposables.Add(
            settingsService
                .SettingsObservable.Select(static settings =>
                    (
                        OnlyShowCurrentPlayerRoom: GetScenarioItemsDockSettings(settings).OnlyShowCurrentPlayerRoom,
                        ShowGameplayUiDuringTransitions: GetDisplaySettings(settings).ShowGameplayUiDuringTransitions
                    )
                )
                .Subscribe(state =>
                    dispatcherService.PostOnUI(() =>
                        UpdateDisplaySettings(state.OnlyShowCurrentPlayerRoom, state.ShowGameplayUiDuringTransitions)
                    )
                )
        );

        _disposables.Add(
            dataObservable
                .InGameScenarioObservable.Select(static scenario => scenario.Status)
                .DistinctUntilChanged()
                .Subscribe(status => dispatcherService.PostOnUI(() => UpdateScenarioStatus(status)))
        );

        // Defer the initial rebuild so the dock panel can appear immediately.
        // RebuildFilteredRoomGroups() triggers per-group CollectionChanged events that cause
        // Avalonia to realize item controls for all room groups and their items synchronously,
        // which can take 1-2 s for large item pools and must not block the constructor.
        dispatcherService.PostOnUI(RebuildFilteredRoomGroups);
    }

    public void Dispose()
    {
        ((INotifyCollectionChanged)_source.RoomGroups).CollectionChanged -= OnSourceRoomGroupsChanged;
        _disposables.Dispose();
        RoomGroups.Dispose();
    }

    private void OnSourceRoomGroupsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        RebuildFilteredRoomGroups();

    private void UpdateDisplaySettings(bool onlyShowCurrentPlayerRoom, bool showGameplayUiDuringTransitions)
    {
        if (
            _onlyShowCurrentPlayerRoom == onlyShowCurrentPlayerRoom
            && _showGameplayUiDuringTransitions == showGameplayUiDuringTransitions
        )
            return;

        _onlyShowCurrentPlayerRoom = onlyShowCurrentPlayerRoom;
        _showGameplayUiDuringTransitions = showGameplayUiDuringTransitions;
        RebuildFilteredRoomGroups();
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
        RebuildFilteredRoomGroups();
    }

    private void UpdateScenarioStatus(ScenarioStatus scenarioStatus)
    {
        if (_scenarioStatus == scenarioStatus)
            return;

        _scenarioStatus = scenarioStatus;
        RebuildFilteredRoomGroups();
    }

    private void RebuildFilteredRoomGroups()
    {
        List<ScenarioRoomGroupViewModel> filteredGroups = [];

        foreach (ScenarioRoomGroupViewModel roomGroup in _source.RoomGroups)
        {
            if (ShouldInclude(roomGroup))
                filteredGroups.Add(roomGroup);
        }

        bool isGameplayActive = _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions);
        bool hasRoomGroups = isGameplayActive && filteredGroups.Count > 0;
        string emptyMessage =
            hasRoomGroups ? string.Empty
            : isGameplayActive && _onlyShowCurrentPlayerRoom && _hasCurrentPlayerRoom ? CurrentRoomEmptyMessage
            : DefaultEmptyMessage;

        ApplyFilteredGroupChanges(filteredGroups);

        HasRoomGroups = hasRoomGroups;
        EmptyMessage = emptyMessage;
    }

    /// <summary>
    /// Incrementally patches <see cref="_roomGroups"/> so only changed positions
    /// fire collection-changed events, avoiding a full Reset that rebuilds the
    /// entire ItemsControl visual tree.
    /// </summary>
    private void ApplyFilteredGroupChanges(List<ScenarioRoomGroupViewModel> filteredGroups)
    {
        while (_roomGroups.Count > filteredGroups.Count)
            _roomGroups.RemoveAt(_roomGroups.Count - 1);

        for (int i = 0; i < filteredGroups.Count; i++)
        {
            if (i < _roomGroups.Count)
            {
                if (!ReferenceEquals(_roomGroups[i], filteredGroups[i]))
                    _roomGroups[i] = filteredGroups[i];
            }
            else
            {
                _roomGroups.Add(filteredGroups[i]);
            }
        }
    }

    private bool ShouldInclude(ScenarioRoomGroupViewModel roomGroup)
    {
        if (!_onlyShowCurrentPlayerRoom || !_hasCurrentPlayerRoom)
            return true;

        foreach (ScenarioItemSlotViewModel item in roomGroup.Items)
        {
            if (item.RoomId == _currentPlayerRoomId)
                return true;
        }

        return false;
    }

    private static ScenarioItemsDockSettings GetScenarioItemsDockSettings(OutbreakTrackerSettings settings) =>
        (settings.Display ?? new DisplaySettings()).ScenarioItemsDock ?? new ScenarioItemsDockSettings();

    private static DisplaySettings GetDisplaySettings(OutbreakTrackerSettings settings) =>
        settings.Display ?? new DisplaySettings();
}

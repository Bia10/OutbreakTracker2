using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

public sealed partial class MapCanvasViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<MapCanvasViewModel> _logger;
    private readonly IPolygonCirclePackingService _polygonCirclePackingService;
    private readonly ScenarioItemsViewModel _scenarioItemsViewModel;
    private DisposableBag _disposables;
    private bool _hasActivePlayers;
    private byte _localPlayerSlotIndex = byte.MaxValue;
    private MapProjectionCalibration _mapProjectionCalibration = MapProjectionCalibration.Default;
    private string? _mapBackgroundRelativePath;
    private short _roomId = -1;
    private string _roomName = string.Empty;
    private string _scenarioName = string.Empty;
    private ScenarioStatus _scenarioStatus;
    private bool _projectAllScenarioItemsOntoMap;
    private bool _showGameplayUiDuringTransitions;

    public Observable<DecodedInGamePlayer[]> PlayersObservable { get; }
    public Observable<DecodedEnemy[]> EnemiesObservable { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMapBackgroundImage))]
    private Bitmap? _mapBackgroundImage;

    [ObservableProperty]
    private bool _isInGame;

    [ObservableProperty]
    private double _mapWidth = 800;

    [ObservableProperty]
    private double _mapHeight = 600;

    public bool HasMapBackgroundImage => MapBackgroundImage is not null;

    public byte LocalPlayerSlotIndex => _localPlayerSlotIndex;

    public MapProjectionCalibration MapProjectionCalibration => _mapProjectionCalibration;

    public string? MapBackgroundRelativePath => _mapBackgroundRelativePath;

    public short RoomId => _roomId;

    public string RoomName => _roomName;

    public string ScenarioName => _scenarioName;

    public ScenarioItemsViewModel ScenarioItemsViewModel => _scenarioItemsViewModel;

    public bool ProjectAllScenarioItemsOntoMap => _projectAllScenarioItemsOntoMap;

    public MapCanvasViewModel(
        IDataObservableSource dataObservable,
        IPolygonCirclePackingService polygonCirclePackingService,
        ScenarioItemsViewModel scenarioItemsViewModel,
        IDispatcherService dispatcherService,
        IAppSettingsService settingsService,
        ILogger<MapCanvasViewModel> logger
    )
    {
        _logger = logger;
        _polygonCirclePackingService = polygonCirclePackingService;
        _scenarioItemsViewModel = scenarioItemsViewModel;
        ScenarioItemsDockSettings initialScenarioItemsDockSettings = GetScenarioItemsDockSettings(
            settingsService.Current
        );
        _showGameplayUiDuringTransitions = GetDisplaySettings(settingsService.Current).ShowGameplayUiDuringTransitions;
        _projectAllScenarioItemsOntoMap = initialScenarioItemsDockSettings.ProjectAllOntoMap;
        PlayersObservable = dataObservable.InGamePlayersObservable;
        EnemiesObservable = dataObservable.EnemiesObservable;

        _disposables.Add(
            dataObservable
                .InGamePlayersObservable.ObserveOnThreadPool()
                .Subscribe(
                    onNext: players => dispatcherService.PostOnUI(() => UpdatePlayers(players)),
                    onErrorResume: ex => _logger.LogError(ex, "Error while monitoring map canvas player state"),
                    onCompleted: _ => _logger.LogInformation("Map canvas player-state stream completed")
                )
        );

        _disposables.Add(
            dataObservable
                .InGameScenarioObservable.Select(static scenario => new ScenarioMapState(
                    scenario.Status,
                    scenario.ScenarioName,
                    scenario.LocalPlayerSlotIndex
                ))
                .DistinctUntilChanged()
                .Subscribe(
                    onNext: scenario => dispatcherService.PostOnUI(() => UpdateScenarioState(scenario)),
                    onErrorResume: ex => _logger.LogError(ex, "Error while monitoring map canvas scenario state"),
                    onCompleted: _ => _logger.LogInformation("Map canvas scenario-state stream completed")
                )
        );

        _disposables.Add(
            settingsService
                .SettingsObservable.Select(static settings =>
                    (
                        ShowGameplayUiDuringTransitions: GetDisplaySettings(settings).ShowGameplayUiDuringTransitions,
                        ProjectAllScenarioItemsOntoMap: GetScenarioItemsDockSettings(settings).ProjectAllOntoMap
                    )
                )
                .DistinctUntilChanged()
                .Subscribe(
                    onNext: state =>
                        dispatcherService.PostOnUI(() =>
                            UpdateDisplaySettings(
                                state.ShowGameplayUiDuringTransitions,
                                state.ProjectAllScenarioItemsOntoMap
                            )
                        ),
                    onErrorResume: ex => _logger.LogError(ex, "Error while monitoring map canvas settings"),
                    onCompleted: _ => _logger.LogInformation("Map canvas settings stream completed")
                )
        );
    }

    public void Dispose() => _disposables.Dispose();

    public void AdjustProjectionOffset(double deltaX, double deltaY)
    {
        if (string.IsNullOrWhiteSpace(_scenarioName))
            return;

        SaveProjectionCalibration(_mapProjectionCalibration.WithOffsetDelta(deltaX, deltaY));
    }

    public void AdjustProjectionScale(double multiplier)
    {
        if (string.IsNullOrWhiteSpace(_scenarioName))
            return;

        SaveProjectionCalibration(_mapProjectionCalibration.WithScaleMultiplier(multiplier));
    }

    public void ResetProjectionCalibration()
    {
        UpdateProjectionCalibration(
            MapProjectionCalibrationStore.ResetCalibration(_scenarioName, _mapBackgroundRelativePath)
        );
    }

    private void UpdatePlayers(DecodedInGamePlayer[] players)
    {
        _hasActivePlayers = false;
        _roomName = string.Empty;
        _roomId = -1;

        foreach (DecodedInGamePlayer player in players)
        {
            if (!player.IsEnabled || !player.IsInGame)
                continue;

            _hasActivePlayers = true;
            break;
        }

        if (TryGetTrackedPlayer(players, out DecodedInGamePlayer trackedPlayer))
        {
            _roomName = trackedPlayer.RoomName;
            _roomId = trackedPlayer.RoomId;
        }

        UpdateVisibleState();
        UpdateMapBackground();
    }

    private void UpdateScenarioState(ScenarioMapState scenario)
    {
        _scenarioStatus = scenario.Status;
        _scenarioName = scenario.ScenarioName;
        _localPlayerSlotIndex = scenario.LocalPlayerSlotIndex;
        UpdateVisibleState();
        UpdateMapBackground();
    }

    public IReadOnlyList<ScenarioItemSlotViewModel> GetProjectedScenarioItems()
    {
        List<ScenarioItemSlotViewModel> projectedItems = [];

        foreach (ScenarioItemSlotViewModel item in _scenarioItemsViewModel.Items)
        {
            if (item.IsEmpty || item.IsHeldByPlayer || item.RoomId == 0)
                continue;

            if (_projectAllScenarioItemsOntoMap || item.IsProjectedOnMap)
                projectedItems.Add(item);
        }

        if (projectedItems.Count <= 1)
            return projectedItems;

        IReadOnlyList<int> visibleIndices = ScenarioItemRoomGroupProjection.GetVisibleIndices(projectedItems);
        if (visibleIndices.Count == projectedItems.Count)
            return projectedItems;

        List<ScenarioItemSlotViewModel> deduplicatedItems = new(visibleIndices.Count);
        foreach (int index in visibleIndices)
            deduplicatedItems.Add(projectedItems[index]);

        return deduplicatedItems;
    }

    internal IReadOnlyList<ScenarioMapItemPlacement> GetProjectedScenarioItemPlacements(MapSectionGeometry section) =>
        ScenarioMapItemPacker.CreateLayout(section, GetProjectedScenarioItems(), _polygonCirclePackingService);

    internal ValueTask<IReadOnlyList<ScenarioMapItemPlacement>> GetProjectedScenarioItemPlacementsAsync(
        MapSectionGeometry section,
        CancellationToken cancellationToken = default
    ) =>
        ScenarioMapItemPacker.CreateLayoutAsync(
            section,
            GetProjectedScenarioItems(),
            _polygonCirclePackingService,
            cancellationToken
        );

    private void UpdateDisplaySettings(bool showGameplayUiDuringTransitions, bool projectAllScenarioItemsOntoMap)
    {
        bool changed = false;

        if (_projectAllScenarioItemsOntoMap != projectAllScenarioItemsOntoMap)
        {
            _projectAllScenarioItemsOntoMap = projectAllScenarioItemsOntoMap;
            OnPropertyChanged(nameof(ProjectAllScenarioItemsOntoMap));
            changed = true;
        }

        if (_showGameplayUiDuringTransitions == showGameplayUiDuringTransitions)
        {
            if (changed)
                OnPropertyChanged(nameof(ScenarioItemsViewModel));

            return;
        }

        _showGameplayUiDuringTransitions = showGameplayUiDuringTransitions;
        UpdateVisibleState();

        if (changed)
            OnPropertyChanged(nameof(ScenarioItemsViewModel));
    }

    private void UpdateVisibleState() =>
        IsInGame = _hasActivePlayers && _scenarioStatus.ShouldShowGameplayUi(_showGameplayUiDuringTransitions);

    private void UpdateMapBackground()
    {
        string? relativePath = ScenarioMapAssetResolver.ResolveRelativePath(_scenarioName, _roomName, _roomId);
        if (string.Equals(_mapBackgroundRelativePath, relativePath, StringComparison.OrdinalIgnoreCase))
        {
            LoadProjectionCalibration(relativePath);
            MapBackgroundImage = ScenarioMapAssetResolver.LoadBitmap(_scenarioName, relativePath, _roomId, _roomName);
            return;
        }

        _mapBackgroundRelativePath = relativePath;
        OnPropertyChanged(nameof(MapBackgroundRelativePath));
        LoadProjectionCalibration(relativePath);
        MapBackgroundImage = ScenarioMapAssetResolver.LoadBitmap(_scenarioName, relativePath, _roomId, _roomName);
    }

    private void LoadProjectionCalibration(string? relativePath) =>
        UpdateProjectionCalibration(MapProjectionCalibrationStore.Resolve(_scenarioName, relativePath));

    private void SaveProjectionCalibration(MapProjectionCalibration calibration) =>
        UpdateProjectionCalibration(
            MapProjectionCalibrationStore.SaveCalibration(_scenarioName, _mapBackgroundRelativePath, calibration)
        );

    private void UpdateProjectionCalibration(MapProjectionCalibration calibration)
    {
        if (_mapProjectionCalibration == calibration)
            return;

        _mapProjectionCalibration = calibration;
        OnPropertyChanged(nameof(MapProjectionCalibration));
    }

    private bool TryGetTrackedPlayer(DecodedInGamePlayer[] players, out DecodedInGamePlayer trackedPlayer)
    {
        if (_localPlayerSlotIndex < players.Length)
        {
            DecodedInGamePlayer localPlayer = players[_localPlayerSlotIndex];
            if (localPlayer.IsEnabled && localPlayer.IsInGame)
            {
                trackedPlayer = localPlayer;
                return true;
            }
        }

        foreach (DecodedInGamePlayer player in players)
        {
            if (!player.IsEnabled || !player.IsInGame)
                continue;

            trackedPlayer = player;
            return true;
        }

        trackedPlayer = default!;
        return false;
    }

    private static DisplaySettings GetDisplaySettings(OutbreakTrackerSettings settings) =>
        settings.Display ?? new DisplaySettings();

    private static ScenarioItemsDockSettings GetScenarioItemsDockSettings(OutbreakTrackerSettings settings) =>
        (settings.Display ?? new DisplaySettings()).ScenarioItemsDock ?? new ScenarioItemsDockSettings();

    private readonly record struct ScenarioMapState(
        ScenarioStatus Status,
        string ScenarioName,
        byte LocalPlayerSlotIndex
    );
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Utilities;
using OutbreakTracker2.Application.Views.Common.ScenarioImg;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoomPlayer;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoomPlayer.Factory;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoom;

public sealed partial class LobbyRoomViewModel : ObservableObject, IAsyncDisposable
{
    private readonly ILogger<LobbyRoomViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private DisposableBag _subscriptions;
    private readonly CancellationTokenSource _imageUpdateCts = new();

    // Keyed by the decoded player's stable Ulid so VMs are reused by slot identity,
    // not by their own transient object identity.
    private readonly Dictionary<Ulid, LobbyRoomPlayerViewModel> _viewModelCache = [];
    private readonly ObservableList<LobbyRoomPlayerViewModel> _playersInternal = [];
    private byte _currentRoomMasterId = 255;

    public NotifyCollectionChangedSynchronizedViewList<LobbyRoomPlayerViewModel> PlayersView { get; }

    public ScenarioImageViewModel ScenarioImageViewModel { get; }

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _timeLeft = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayersDisplay))]
    private short _maxPlayer = GameConstants.MaxPlayers;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayersDisplay))]
    private short _curPlayer;

    [ObservableProperty]
    private string _difficulty = string.Empty;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    [ObservableProperty]
    private bool _isAtLobby;

    public string PlayersDisplay => $"{CurPlayer}/{MaxPlayer}";

    public LobbyRoomViewModel(
        IDataObservableSource dataObservable,
        ILogger<LobbyRoomViewModel> logger,
        IDispatcherService dispatcherService,
        ILobbyRoomPlayerViewModelFactory playerVmFactory,
        IScenarioImageViewModelFactory scenarioImageVmFactory
    )
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        ScenarioImageViewModel = scenarioImageVmFactory.Create();

        PlayersView = _playersInternal.ToNotifyCollectionChanged(
            SynchronizationContextCollectionEventDispatcher.Current
        );

        try
        {
            _subscriptions.Add(
                dataObservable
                    .IsAtLobbyObservable.ObserveOnThreadPool()
                    .SubscribeAwait(
                        async (isAtLobby, cancellationToken) =>
                        {
                            await _dispatcherService
                                .InvokeOnUIAsync(() => IsAtLobby = isAtLobby, cancellationToken)
                                .ConfigureAwait(false);
                        },
                        AwaitOperation.Drop
                    )
            );

            _subscriptions.Add(
                dataObservable
                    .LobbyRoomObservable.WithLatestFrom(
                        dataObservable.IsAtLobbyObservable,
                        (lobbyData, isAtLobby) => (LobbyData: lobbyData, IsAtLobby: isAtLobby)
                    )
                    .Where(static state => state.IsAtLobby)
                    .Select(static state => state.LobbyData)
                    .ObserveOnThreadPool()
                    .SubscribeAwait(
                        async (lobbyData, cancellationToken) =>
                        {
                            try
                            {
                                await _dispatcherService
                                    .InvokeOnUIAsync(() => UpdateLobbyProperties(lobbyData), cancellationToken)
                                    .ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error during lobby room data processing cycle");
                            }
                        },
                        AwaitOperation.Drop
                    )
            );

            _subscriptions.Add(
                dataObservable
                    .LobbyRoomPlayersObservable.WithLatestFrom(
                        dataObservable.IsAtLobbyObservable,
                        (players, isAtLobby) => (Players: players, IsAtLobby: isAtLobby)
                    )
                    .Where(static state => state.IsAtLobby)
                    .Select(static state => state.Players)
                    .ObserveOnThreadPool()
                    .SubscribeAwait(
                        async (incomingPlayersSnapshot, cancellationToken) =>
                        {
                            List<DecodedLobbyRoomPlayer> filteredIncomingPlayers = incomingPlayersSnapshot
                                .AsValueEnumerable()
                                .Where(IsPlayerActive)
                                .ToList();

                            try
                            {
                                OrderedObservableListReconcilePlan<
                                    DecodedLobbyRoomPlayer,
                                    LobbyRoomPlayerViewModel,
                                    Ulid
                                > plan = OrderedObservableListReconciler.BuildPlan(
                                    filteredIncomingPlayers,
                                    _viewModelCache,
                                    static player => player.Id,
                                    playerVmFactory.Create
                                );

                                await _dispatcherService
                                    .InvokeOnUIAsync(
                                        () =>
                                        {
                                            List<LobbyRoomPlayerViewModel> removedViewModels =
                                                GetRemovedPlayerViewModels(plan.CacheKeysToRemove);

                                            OrderedObservableListReconciler.ApplyPlan(
                                                plan,
                                                _playersInternal,
                                                _viewModelCache,
                                                static vm => vm.ViewModelId,
                                                (vm, player) => vm.Update(player, _currentRoomMasterId)
                                            );

                                            DisposePlayerViewModels(removedViewModels);

                                            _logger.LogDebug(
                                                "Lobby room players updated: {Count}",
                                                _playersInternal.Count
                                            );
                                        },
                                        cancellationToken
                                    )
                                    .ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error during lobby room players snapshot processing cycle");
                            }
                        },
                        AwaitOperation.Drop
                    )
            );
        }
        catch
        {
            _subscriptions.Dispose();
            PlayersView.Dispose();
            ScenarioImageViewModel.Dispose();
            throw;
        }
    }

    private static bool IsPlayerActive(DecodedLobbyRoomPlayer player) =>
        !string.IsNullOrEmpty(player.CharacterName)
        || (!string.IsNullOrEmpty(player.NpcName) && player is { IsEnabled: true });

    private List<LobbyRoomPlayerViewModel> GetRemovedPlayerViewModels(IReadOnlyList<Ulid> removedKeys)
    {
        List<LobbyRoomPlayerViewModel> removedViewModels = [];

        foreach (Ulid removedKey in removedKeys)
        {
            if (_viewModelCache.TryGetValue(removedKey, out LobbyRoomPlayerViewModel? removedViewModel))
                removedViewModels.Add(removedViewModel);
        }

        return removedViewModels;
    }

    private static void DisposePlayerViewModels(IEnumerable<LobbyRoomPlayerViewModel> playerViewModels)
    {
        foreach (LobbyRoomPlayerViewModel playerViewModel in playerViewModels)
            playerViewModel.Dispose();
    }

    private void UpdateLobbyProperties(in DecodedLobbyRoom model)
    {
        if (model.CurPlayer is <= 0 or > 4)
            return;

        Status = model.Status;
        TimeLeft = model.TimeLeft;
        MaxPlayer = model.MaxPlayer;
        CurPlayer = model.CurPlayer;
        Difficulty = model.Difficulty;
        ScenarioName = model.ScenarioName;
        _currentRoomMasterId = model.RoomMasterId;

        if (!string.IsNullOrEmpty(ScenarioName))
        {
            if (EnumUtility.TryParseByValueOrMember(ScenarioName, out Scenario scenarioType))
            {
                _ = TrackScenarioImageUpdateAsync(
                    ScenarioImageViewModel.UpdateImageAsync(scenarioType),
                    ScenarioName,
                    _imageUpdateCts.Token
                );
            }
            else
            {
                _logger.LogWarning(
                    "ScenarioName '{ScenarioName}' could not be parsed to a ScenarioType. Displaying default image",
                    ScenarioName
                );
                _ = TrackScenarioImageUpdateAsync(
                    ScenarioImageViewModel.UpdateToDefaultImageAsync(),
                    ScenarioName,
                    _imageUpdateCts.Token
                );
            }
        }
    }

    private async Task TrackScenarioImageUpdateAsync(
        ValueTask updateTask,
        string scenarioName,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await updateTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update scenario image for lobby room {ScenarioName}", scenarioName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing LobbyRoomViewModel asynchronously");

        _imageUpdateCts.Cancel();
        _imageUpdateCts.Dispose();
        _subscriptions.Dispose();
        ScenarioImageViewModel.Dispose();

        await _dispatcherService
            .InvokeOnUIAsync(() =>
            {
                DisposePlayerViewModels(_viewModelCache.Values);
                _playersInternal.Clear();
                _viewModelCache.Clear();
                PlayersView.Dispose();
                _logger.LogDebug("LobbyRoomViewModel collections cleared on UI thread during async dispose");
            })
            .ConfigureAwait(false);

        _logger.LogDebug("LobbyRoomViewModel asynchronous disposal complete");
    }
}

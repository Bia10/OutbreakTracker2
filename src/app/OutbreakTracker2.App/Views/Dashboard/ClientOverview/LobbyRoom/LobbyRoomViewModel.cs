using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoomPlayer;
using OutbreakTracker2.Extensions;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Models;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoom;

public partial class LobbyRoomViewModel : ObservableObject, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly Dictionary<byte, LobbyRoomPlayerViewModel> _playerViewModelCache = [];
    private readonly ILogger<LobbyRoomViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _timeLeft = string.Empty;

    [ObservableProperty]
    private short _maxPlayer = GameConstants.MaxPlayers;

    [ObservableProperty]
    private short _curPlayer;

    [ObservableProperty]
    private string _difficulty = string.Empty;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    private readonly ObservableList<LobbyRoomPlayerViewModel> _playersInternal = [];
    public NotifyCollectionChangedSynchronizedViewList<LobbyRoomPlayerViewModel> Players { get; }

    public string PlayersDisplay => $"{CurPlayer}/{MaxPlayer}";

    private bool _isDisposed = false;

    public LobbyRoomViewModel(
        IDataManager dataManager,
        ILogger<LobbyRoomViewModel> logger,
        IDispatcherService dispatcherService)
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _logger.LogInformation("Initializing LobbyRoomViewModel");

        Players = _playersInternal
            .ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        dataManager.LobbyRoomObservable
            .ObserveOnThreadPool()
            .SubscribeAwait(async (lobbyData, cancellationToken) =>
            {
                _logger.LogInformation("Processing lobby room data on thread pool.");
                try
                {
                    await _dispatcherService.InvokeOnUIAsync(() =>
                    {
                        if (_isDisposed)
                        {
                            _logger.LogWarning("LobbyRoomViewModel is disposed. Skipping lobby property updates.");
                            return;
                        }
                        _logger.LogTrace("Updating LobbyRoomViewModel properties on UI thread.");
                        UpdateLobbyProperties(lobbyData);
                    }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Lobby room data processing cancelled.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during lobby room data processing cycle.");
                }
            },awaitOperation: AwaitOperation.Drop)
            .AddTo(_disposables);

        dataManager.LobbyRoomPlayersObservable
            .ObserveOnThreadPool()
            .SubscribeAwait(async (incomingPlayers, cancellationToken) =>
            {
                _logger.LogInformation("Processing lobby room players snapshot on thread pool.");

                try
                {
                    var desiredPlayerViewModels = new List<LobbyRoomPlayerViewModel>(incomingPlayers.Length);
                    var incomingPlayerIds = new HashSet<byte>();

                    foreach (DecodedLobbyRoomPlayer incomingPlayer in incomingPlayers.AsValueEnumerable())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        byte playerIdKey = incomingPlayer.NameId;
                        incomingPlayerIds.Add(playerIdKey);

                        if (_playerViewModelCache.TryGetValue(playerIdKey, out LobbyRoomPlayerViewModel? existingVm))
                        {
                            desiredPlayerViewModels.Add(existingVm);
                            _logger.LogTrace("Found existing LobbyRoomPlayerViewModel in cache on TP for PlayerId {PlayerId}", playerIdKey);
                        }
                        else
                        {
                            _logger.LogDebug("Creating new LobbyRoomPlayerViewModel on TP for PlayerId {PlayerId}", playerIdKey);
                            var newVm = new LobbyRoomPlayerViewModel(incomingPlayer);
                            _playerViewModelCache.Add(playerIdKey, newVm);
                            desiredPlayerViewModels.Add(newVm);
                        }
                    }

                    var playerIdsToRemoveFromCache = _playerViewModelCache.Keys.Except(incomingPlayerIds).ToList();
                    foreach (byte playerId in playerIdsToRemoveFromCache)
                    {
                         _logger.LogDebug("Removing LobbyRoomPlayerViewModel from Cache for PlayerId: {PlayerId}", playerId);
                        _playerViewModelCache.Remove(playerId);
                    }

                    desiredPlayerViewModels.Sort((vm1, vm2) => vm1.NameId.CompareTo(vm2.NameId));

                    _logger.LogInformation("Player ViewModel preparation complete on thread pool. {DesiredCount} desired player VMs.", desiredPlayerViewModels.Count);

                    await _dispatcherService.InvokeOnUIAsync(() =>
                    {
                        if (_isDisposed)
                        {
                            _logger.LogWarning("LobbyRoomViewModel is disposed. Skipping player list synchronization on UI thread.");
                            return;
                        }

                        _logger.LogInformation("Applying player updates on UI thread.");

                        foreach (LobbyRoomPlayerViewModel vm in desiredPlayerViewModels)
                        {
                            DecodedLobbyRoomPlayer? playerModel = incomingPlayers.FirstOrDefault(p => p.NameId == vm.UniquePlayerId);
                            if (playerModel != null)
                            {
                                vm.Update(playerModel);
                                _logger.LogTrace("Updating LobbyRoomPlayerViewModel properties on UI thread for PlayerId {PlayerId}", vm.UniquePlayerId);
                            }
                        }

                        _playersInternal.ReplaceAll(desiredPlayerViewModels);
                        _logger.LogInformation("UI list synchronized by clear and add. New count: {Count}", _playersInternal.Count);

                        if (_playersInternal.Count != desiredPlayerViewModels.Count)
                        {
                            _logger.LogWarning("_playersInternal count ({InternalCount}) differs from desiredPlayerViewModels count ({DesiredCount}) after sync. This indicates a potential sync logic error.", _playersInternal.Count, desiredPlayerViewModels.Count);
                        }

                        _logger.LogInformation("UI update complete. Players UI list count: {Count}", _playersInternal.Count);
                    }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Lobby room players snapshot processing cancelled.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during lobby room players snapshot processing cycle.");
                }
            }, awaitOperation: AwaitOperation.Drop)
            .AddTo(_disposables);
    }

    private void UpdateLobbyProperties(DecodedLobbyRoom model)
    {
        Status = model.Status;
        TimeLeft = model.TimeLeft;
        MaxPlayer = model.MaxPlayer;
        CurPlayer = model.CurPlayer;
        Difficulty = model.Difficulty;
        ScenarioName = model.ScenarioName;

        OnPropertyChanged(nameof(PlayersDisplay));
        OnPropertyChanged(nameof(Status));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _logger.LogDebug("Disposing LobbyRoomViewModel.");
        _disposables.Dispose();

        _dispatcherService.PostOnUI(() =>
        {
            if (_isDisposed)
            {
                _playersInternal.Clear();
                _playerViewModelCache.Clear();
                _logger.LogDebug("LobbyRoomViewModel collections cleared on UI thread during dispose.");
            }
        });

        _logger.LogDebug("LobbyRoomViewModel disposal complete.");
    }
}

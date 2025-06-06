using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayer.Factory;
using OutbreakTracker2.Outbreak.Models;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZLinq;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayers;

public class InGamePlayersViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IDisposable _subscription;
    private readonly ILogger<InGamePlayersViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly Dictionary<string, InGamePlayerViewModel> _viewModelCache = [];
    private readonly ObservableList<InGamePlayerViewModel> _players = [];
    public NotifyCollectionChangedSynchronizedViewList<InGamePlayerViewModel> PlayersView { get; }

    public InGamePlayersViewModel(
        IDataManager dataManager,
        ILogger<InGamePlayersViewModel> logger,
        IDispatcherService dispatcherService,
        IInGamePlayerViewModelFactory inGamePlayerViewModelFactory)
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _logger.LogInformation("Initializing InGamePlayersViewModel");

        PlayersView = _players
            .ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = dataManager.InGamePlayersObservable
            .ObserveOnThreadPool()
            .SubscribeAwait(async (incomingPlayersSnapshot, ct) =>
            {
                _logger.LogInformation("Processing players snapshot on thread pool with {Length} entries", incomingPlayersSnapshot.Length);

                try
                {
                    List<DecodedInGamePlayer> filteredIncomingPlayers = incomingPlayersSnapshot
                        .AsValueEnumerable()
                        .Where(IsPlayerActive)
                        .ToList();

                    _logger.LogInformation("Processed {Count} filtered player entries on thread pool", filteredIncomingPlayers.Count);

                    Dictionary<string, DecodedInGamePlayer> incomingPlayerDataMap = filteredIncomingPlayers
                        .AsValueEnumerable()
                        .ToDictionary(player => player.NameId > 0 ? $"NameId_{player.NameId}" : $"Ulid_{player.Id}");


                    List<InGamePlayerViewModel> desiredViewModels = new(filteredIncomingPlayers.Count);

                    foreach (DecodedInGamePlayer incomingPlayer in filteredIncomingPlayers.AsValueEnumerable())
                    {
                        ct.ThrowIfCancellationRequested();
                        string playerUniqueId = incomingPlayer.NameId > 0
                            ? $"NameId_{incomingPlayer.NameId}"
                            : $"Ulid_{incomingPlayer.Id}";

                        if (_viewModelCache.TryGetValue(playerUniqueId, out InGamePlayerViewModel? existingVm))
                        {
                            desiredViewModels.Add(existingVm);
                            _logger.LogTrace("Found existing ViewModel in cache on TP for {UniqueId}", playerUniqueId);
                        }
                        else
                        {
                            _logger.LogDebug("Creating new ViewModel on TP for {UniqueId}", playerUniqueId);
                            InGamePlayerViewModel newVm = inGamePlayerViewModelFactory.Create(incomingPlayer);
                            desiredViewModels.Add(newVm);
                        }
                    }

                    _logger.LogInformation("ViewModel preparation complete on thread pool. {DesiredCount} desired VMs", desiredViewModels.Count);

                    await _dispatcherService.InvokeOnUIAsync(() =>
                    {
                        _logger.LogInformation("Applying player updates on UI thread");

                        foreach (InGamePlayerViewModel vm in desiredViewModels)
                            if (incomingPlayerDataMap.TryGetValue(vm.UniqueNameId, out DecodedInGamePlayer? playerData))
                            {
                                vm.Update(playerData);
                                _logger.LogTrace("Updating ViewModel properties on UI thread for {UniqueId}", vm.UniqueNameId);
                            }
                            else
                            {
                                _logger.LogWarning("Player data not found in map for VM {UniqueId} during UI update. This should not happen", vm.UniqueNameId);
                            }

                        HashSet<string> desiredUniqueIdsLookup = new(desiredViewModels.Select<InGamePlayerViewModel, string>(vm => vm.UniqueNameId), StringComparer.Ordinal);
                        for (int i = _players.Count - 1; i >= 0; i--)
                        {
                            InGamePlayerViewModel currentVmInList = _players[i];
                            if (desiredUniqueIdsLookup.Contains(currentVmInList.UniqueNameId))
                                continue;

                            _logger.LogDebug("Removing ViewModel from Players list & Cache on UI thread for UniqueId: {UniqueId}", currentVmInList.UniqueNameId);
                            _players.RemoveAt(i);
                            _viewModelCache.Remove(currentVmInList.UniqueNameId);
                        }

                        for (int i = 0; i < desiredViewModels.Count; i++)
                        {
                            InGamePlayerViewModel desiredVm = desiredViewModels[i];

                            if (_viewModelCache.TryAdd(desiredVm.UniqueNameId, desiredVm))
                                _logger.LogDebug("Added new ViewModel to cache on UI thread: {UniqueId}", desiredVm.UniqueNameId);

                            int currentIndexInPlayers;
                            if (i < _players.Count && _players[i].UniqueNameId.Equals(desiredVm.UniqueNameId, StringComparison.Ordinal))
                            {
                                currentIndexInPlayers = i;
                                _logger.LogTrace("ViewModel {UniqueId} already in correct position", desiredVm.UniqueNameId);
                            }
                            else
                            {
                                currentIndexInPlayers = _players.IndexOf(desiredVm);
                            }

                            if (currentIndexInPlayers is -1)
                            {
                                _logger.LogDebug("Inserting ViewModel into ObservableList on UI thread: {UniqueId} at index {Index}",
                                    desiredVm.UniqueNameId, i);
                                _players.Insert(i, desiredVm);
                            }
                            else if (currentIndexInPlayers != i)
                            {
                                _logger.LogDebug("Moving ViewModel in ObservableList on UI thread: {UniqueId} from index {FromIndex} to index {ToIndex}",
                                    desiredVm.UniqueNameId, currentIndexInPlayers, i);
                                _players.Move(currentIndexInPlayers, i);
                            }
                        }

                        if (_players.Count != desiredViewModels.Count)
                            _logger.LogWarning("_players count ({PCount}) differs from desiredViewModels count ({DCount}) after sync. Adjusting",
                                _players.Count, desiredViewModels.Count);

                        _logger.LogInformation("UI update complete. Players ObservableList count: {Count}", _players.Count);
                    }, ct).ConfigureAwait(false);

                    _logger.LogInformation("Finished processing player snapshot cycle");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Player snapshot processing cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during player snapshot processing cycle");
                }
            }, AwaitOperation.Drop);
    }

    private static bool IsPlayerActive(DecodedInGamePlayer? player)
    {
        if (player is null) return false;

        return player.NameId > 0 || (player.NameId is 0 && !string.IsNullOrEmpty(player.Type));
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing InGamePlayersViewModel");
        _subscription.Dispose();

        await _dispatcherService.InvokeOnUIAsync(() =>
        {
            _players.Clear();
            _viewModelCache.Clear();
            _logger.LogDebug("InGamePlayersViewModel collections cleared on UI thread during async dispose");
        }).ConfigureAwait(false);

        _logger.LogDebug("InGamePlayersViewModel asynchronous disposal complete");
    }
}
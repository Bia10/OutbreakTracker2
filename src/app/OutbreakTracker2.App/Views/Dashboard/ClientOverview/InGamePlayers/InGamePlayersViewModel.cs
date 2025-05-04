using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using R3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using ZLinq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayers;

public class InGamePlayersViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly Dictionary<byte, InGamePlayerViewModel> _viewModelCache = new();
    private readonly ILogger<InGamePlayersViewModel> _logger;
    private readonly IDataManager _dataManager;

    public ObservableCollection<InGamePlayerViewModel> Players { get; } = new();

    public InGamePlayersViewModel(IDataManager dataManager, ILogger<InGamePlayersViewModel> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
        _logger.LogInformation("Initializing InGamePlayersViewModel");

        _subscription = _dataManager.InGamePlayersObservable
            .Select(players => players.ToList())
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(UpdatePlayers);
    }

    private void UpdatePlayers(List<DecodedInGamePlayer> updatedPlayers)
    {
        _logger.LogInformation("Updating players with {Count} unique entries", updatedPlayers.Count);

        var currentIds = new HashSet<byte>(_viewModelCache.Keys);
        var newIds = new HashSet<byte>(updatedPlayers.Select(p => p.NameId));
        foreach (byte id in currentIds.Where(id => !newIds.Contains(id)).ToList())
            if (_viewModelCache.Remove(id, out InGamePlayerViewModel? vm))
                Players.Remove(vm);

        int targetIndex = 0;
        foreach (DecodedInGamePlayer player in updatedPlayers.Where(player => !string.IsNullOrEmpty(player.CharacterName)))
        {
            _logger.LogInformation("Updating player {Name}", player.CharacterName);


            if (_viewModelCache.TryGetValue(player.NameId, out InGamePlayerViewModel? existingVm))
            {
                existingVm.Update(player);

                int currentIndex = Players.IndexOf(existingVm);
                if (currentIndex != targetIndex)
                {
                    if (currentIndex >= 0 && currentIndex < Players.Count && targetIndex >= 0 && targetIndex < Players.Count)
                        Players.Move(currentIndex, targetIndex);
                    else
                        _logger.LogError("Invalid move indices. Current: {Current}, Target: {Target}, Count: {Count}", currentIndex, targetIndex, Players.Count);
                }
            }
            else
            {
                var newVm = new InGamePlayerViewModel(player, _dataManager);
                _viewModelCache[player.NameId] = newVm;
                Players.Insert(targetIndex, newVm);
            }

            targetIndex++;
        }
    }


    public void Dispose()
    {
        _subscription.Dispose();
        _viewModelCache.Clear();
        Players.Clear();
        GC.SuppressFinalize(this);
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayer;
using OutbreakTracker2.Outbreak.Models;
using R3;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayers;

public class InGamePlayersViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly Dictionary<byte, InGamePlayerViewModel> _viewModelCache = new();
    private readonly ILogger<InGamePlayersViewModel> _logger;

    public ObservableCollection<InGamePlayerViewModel> Players { get; } = new();

    public InGamePlayersViewModel(IDataManager dataManager, ILogger<InGamePlayersViewModel> logger)
    {
        _logger = logger;
        _logger.LogInformation("Initializing InGamePlayersViewModel");

        _subscription = dataManager.InGamePlayersObservable
            .Select(players =>
            {
                var seenIds = new HashSet<byte>(players.Length);
                return players.Where(player => seenIds.Add(player.NameId))
                    .ToList();
            })
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
        foreach (DecodedInGamePlayer player in updatedPlayers)
        {
            if (_viewModelCache.TryGetValue(player.NameId, out InGamePlayerViewModel? existingVm))
            {
                existingVm.Update(player);

                int currentIndex = Players.IndexOf(existingVm);
                if (currentIndex != targetIndex)
                    Players.Move(currentIndex, targetIndex);
            }
            else
            {
                // Add new ViewModel
                var newVm = new InGamePlayerViewModel(player);
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
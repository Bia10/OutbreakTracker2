using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Services.Dispatcher;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Models;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemies;

public class InGameEnemiesViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<InGameEnemiesViewModel> _logger;
    private readonly IDispatcherService _dispatcherService;
    private readonly IDisposable _subscription;
    private readonly Dictionary<string, InGameEnemyViewModel> _viewModelCache = new();
    private readonly ObservableList<InGameEnemyViewModel> _enemies = [];

    public NotifyCollectionChangedSynchronizedViewList<InGameEnemyViewModel> EnemiesView { get; }

    public InGameEnemiesViewModel(
        ILogger<InGameEnemiesViewModel> logger,
        IDispatcherService dispatcherService,
        IDataManager dataManager)
    {
        _logger = logger;
        _dispatcherService = dispatcherService;
        _logger.LogInformation("Initializing InGameEnemiesViewModel");

        EnemiesView = _enemies.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        _subscription = dataManager.EnemiesObservable
            .ObserveOnThreadPool()
            .SubscribeAwait(async (incomingEnemiesSnapshot, ct) =>
            {
                switch (incomingEnemiesSnapshot.Length)
                {
                    case 0: _logger.LogWarning("Received empty or null enemies snapshot. Entries {Length}", incomingEnemiesSnapshot.Length); return;
                    case > GameConstants.MaxEnemies2: _logger.LogWarning("Received more enemies than the in-game limit. Entries {Length}", incomingEnemiesSnapshot.Length); return;
                    default: _logger.LogInformation("Processing enemies snapshot on thread pool with {Length} entries", incomingEnemiesSnapshot.Length);
                        try
                        {
                            var filteredIncomingEnemies = incomingEnemiesSnapshot
                                .AsValueEnumerable()
                                .Where(IsEnemyActive)
                                .ToList();

                            _logger.LogInformation("Processed {Count} filtered enemy entries on thread pool.", filteredIncomingEnemies.Count);

                            var incomingEnemyDataMap = filteredIncomingEnemies
                                .AsValueEnumerable()
                                .ToDictionary(enemy => enemy.Id);

                            var desiredViewModels = new List<InGameEnemyViewModel>(filteredIncomingEnemies.Count);

                            foreach (DecodedEnemy incomingEnemy in filteredIncomingEnemies.AsValueEnumerable())
                            {
                                if (_viewModelCache.TryGetValue(incomingEnemy.Id, out InGameEnemyViewModel? existingVm))
                                {
                                    desiredViewModels.Add(existingVm);
                                    _logger.LogTrace("Found existing ViewModel in cache on TP for {UniqueId}", incomingEnemy.Id);
                                }
                                else
                                {
                                    _logger.LogDebug("Creating new ViewModel on TP for {UniqueId}", incomingEnemy.Id);
                                    var newVm = new InGameEnemyViewModel(incomingEnemy, dataManager); // Pass dataManager
                                    desiredViewModels.Add(newVm);
                                }
                            }

                            _logger.LogInformation("ViewModel preparation complete on thread pool. {DesiredCount} desired VMs.", desiredViewModels.Count);

                            await _dispatcherService.InvokeOnUIAsync(() =>
                            {
                                _logger.LogInformation("Applying enemy updates on UI thread.");

                                foreach (InGameEnemyViewModel vm in desiredViewModels)
                                    if (incomingEnemyDataMap.TryGetValue(vm.UniqueId, out DecodedEnemy? enemyData))
                                    {
                                        vm.Update(enemyData);
                                        _viewModelCache[vm.UniqueId] = vm;
                                        _logger.LogTrace("Updating ViewModel properties and ensuring in cache on UI thread for {UniqueId}", vm.UniqueId);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Enemy data not found in map for VM {UniqueId} during UI update. This should not happen.", vm.UniqueId);
                                    }

                                var desiredUniqueIdsLookup = new HashSet<string>(desiredViewModels.Select(vm => vm.UniqueId));
                                for (int i = _enemies.Count - 1; i >= 0; i--)
                                {
                                    InGameEnemyViewModel currentVmInList = _enemies[i];
                                    if (!desiredUniqueIdsLookup.Contains(currentVmInList.UniqueId))
                                    {
                                        _logger.LogDebug("Removing ViewModel from Enemies list & Cache on UI thread for UniqueId: {UniqueId}", currentVmInList.UniqueId);
                                        _enemies.RemoveAt(i);
                                        _viewModelCache.Remove(currentVmInList.UniqueId);
                                    }
                                }

                                for (int i = 0; i < desiredViewModels.Count; i++)
                                {
                                    InGameEnemyViewModel desiredVm = desiredViewModels[i];

                                    int currentIndexInEnemies;
                                    if (i < _enemies.Count && _enemies[i].UniqueId.Equals(desiredVm.UniqueId, StringComparison.Ordinal))
                                    {
                                        currentIndexInEnemies = i;
                                        _logger.LogTrace("ViewModel {UniqueId} already in correct position.", desiredVm.UniqueId);
                                    }
                                    else
                                    {
                                        currentIndexInEnemies = _enemies.IndexOf(desiredVm);
                                    }

                                    if (currentIndexInEnemies is -1)
                                    {
                                        _logger.LogDebug("Inserting ViewModel into ObservableList on UI thread: {UniqueId} at index {Index}", desiredVm.UniqueId, i);
                                        _enemies.Insert(i, desiredVm);
                                    }
                                    else if (currentIndexInEnemies != i)
                                    {
                                        _logger.LogDebug("Moving ViewModel in ObservableList on UI thread: {UniqueId} from index {FromIndex} to index {ToIndex}", desiredVm.UniqueId, currentIndexInEnemies, i);
                                        _enemies.Move(currentIndexInEnemies, i);
                                    }
                                }

                                if (_enemies.Count != desiredViewModels.Count)
                                {
                                    _logger.LogWarning("_enemies count ({ECount}) differs from desiredViewModels count ({DesCount}) after sync. This indicates an issue in logic.", _enemies.Count, desiredViewModels.Count);

                                }

                                _logger.LogInformation("UI update complete. Enemies ObservableList count: {Count}", _enemies.Count);
                            }, ct);

                            _logger.LogInformation("Finished processing enemy snapshot cycle.");
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogTrace("Enemy snapshot processing cancelled.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during enemy snapshot processing cycle.");
                        }

                        break;
                }
            }, AwaitOperation.Drop);
    }

    /// <summary>
    /// Determines if a DecodedEnemy should be considered active and included for display/tracking.
    /// This filter should be applied early in the processing pipeline.
    /// </summary>
    private static bool IsEnemyActive(DecodedEnemy enemy)
        => !string.IsNullOrEmpty(enemy.Name) && enemy.MaxHp > 0;

    public void Dispose()
    {
        _logger.LogInformation("Disposing InGameEnemiesViewModel");
        _subscription.Dispose();

        _dispatcherService.PostOnUI(() =>
        {
            _enemies.Clear();
            _viewModelCache.Clear();
            _logger.LogDebug("InGameEnemiesViewModel collections cleared on UI thread during dispose.");
        });

        EnemiesView.Dispose();
        _logger.LogInformation("InGameEnemiesViewModel disposed.");
    }
}
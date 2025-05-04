using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Outbreak.Models;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnemyCompositeKey = System.Tuple<short, int, int, int>;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemies;

// TODO: this will need rewerite once better way to uniquely identify enemies is found
public class InGameEnemiesViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly ILogger<InGameEnemiesViewModel> _logger;
    private readonly ObservableList<DecodedEnemy> _sourceEnemiesList = [];

    public NotifyCollectionChangedSynchronizedViewList<InGameEnemyViewModel> EnemiesView { get; }

    public InGameEnemiesViewModel(IDataManager dataManager, ILogger<InGameEnemiesViewModel> logger)
    {
        //_logger = logger;
        //_logger.LogInformation("Initializing InGameEnemiesViewModel");

        //ISynchronizedView<DecodedEnemy, InGameEnemyViewModel> enemiesViewSource
        //    = _sourceEnemiesList.CreateView(enemy => new InGameEnemyViewModel(enemy, dataManager));
        //EnemiesView = enemiesViewSource.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

        //_subscription = dataManager.EnemiesObservable
        //    .Select(enemies => enemies.ToList())
        //    .ObserveOn(SynchronizationContext.Current!)
        //    .Subscribe(onNext: HandleEnemyDataUpdate,
        //        onErrorResume: ex => _logger.LogError(ex, "Error receiving enemy data update from observable."),
        //        onCompleted: _ => _logger.LogInformation("EnemiesObservable completed.")
        //    );
    }

    private void HandleEnemyDataUpdate(List<DecodedEnemy> updatedEnemiesData)
    {
        _logger.LogTrace("Received enemy data update with {Count} raw entries.", updatedEnemiesData.Count);

        UpdateSourceEnemiesList(_sourceEnemiesList, updatedEnemiesData, _logger);

        _logger.LogTrace("Enemy data update handled.");
    }

    /// <summary>
    /// Creates a composite key for grouping DecodedEnemy instances.
    /// Note: This key is NOT guaranteed to be unique.
    /// </summary>
    private static EnemyCompositeKey GetEnemyCompositeKey(DecodedEnemy enemy)
        => new(enemy.SlotId, enemy.RoomId, enemy.TypeId, enemy.NameId);

    /// <summary>
    /// Determines if a DecodedEnemy should be considered active and included for display/tracking.
    /// Adjust this filter to include all mobs you deem relevant, even if their key is not unique.
    /// </summary>
    private static bool IsEnemyActive(DecodedEnemy enemy)
        => !string.IsNullOrEmpty(enemy.Name) && enemy.MaxHp > 0;

    /// <summary>
    /// Orchestrates the update of the source enemy list.
    /// Runs on the UI thread due to the R3 Subscribe's ObserveOn.
    /// </summary>
    private static void UpdateSourceEnemiesList(
        ObservableList<DecodedEnemy> sourceList,
        List<DecodedEnemy> updatedEnemiesData,
        ILogger logger)
    {
        logger.LogTrace("Starting source list update.");

        List<DecodedEnemy> activeUpdatedEnemies = FilterActiveEnemies(updatedEnemiesData, logger);

        lock (sourceList.SyncRoot)
        {
            ILookup<EnemyCompositeKey, DecodedEnemy> currentEnemiesLookup = GroupEnemiesByCompositeKey(sourceList);
            ILookup<EnemyCompositeKey, DecodedEnemy> updatedEnemiesLookup = GroupEnemiesByCompositeKey(activeUpdatedEnemies);

            EnemyChangeSet changeSet = MatchEnemies(sourceList, currentEnemiesLookup, updatedEnemiesLookup, logger);

            ApplyChanges(sourceList, changeSet, logger);
        }

        logger.LogTrace("Source list update finished.");
    }

    /// <summary>
    /// Filters the raw incoming enemy data.
    /// </summary>
    private static List<DecodedEnemy> FilterActiveEnemies(List<DecodedEnemy> rawEnemiesData, ILogger logger)
    {
        var filteredList = rawEnemiesData.Where(IsEnemyActive).ToList();
        logger.LogTrace("Filtered raw enemies from {RawCount} to {FilteredCount} active.", rawEnemiesData.Count, filteredList.Count);
        return filteredList;
    }

    /// <summary>
    /// Groups enemies by their composite key for efficient lookup during matching.
    /// </summary>
    private static ILookup<EnemyCompositeKey, DecodedEnemy> GroupEnemiesByCompositeKey(IEnumerable<DecodedEnemy> enemies)
        => enemies.ToLookup(GetEnemyCompositeKey);

    /// <summary>
    /// Compares the current source list against the updated active list to identify additions, removals, and replacements.
    /// </summary>
    private static EnemyChangeSet MatchEnemies(
        ObservableList<DecodedEnemy> sourceList,
        ILookup<EnemyCompositeKey, DecodedEnemy> currentLookup,
        ILookup<EnemyCompositeKey, DecodedEnemy> updatedLookup,
        ILogger logger)
    {
        var matchedNewEnemies = new HashSet<DecodedEnemy>();
        var enemiesToReplace = new Dictionary<DecodedEnemy, DecodedEnemy>();
        var indicesToRemove = new List<int>();

        logger.LogTrace("Starting enemy matching process.");

        for (int i = sourceList.Count - 1; i >= 0; i--)
        {
            DecodedEnemy currentEnemy = sourceList[i];
            EnemyCompositeKey currentKey = GetEnemyCompositeKey(currentEnemy);

            var potentialNewMatches = updatedLookup[currentKey]
                .Where(newEnemy => !matchedNewEnemies.Contains(newEnemy))
                .ToList();

            DecodedEnemy? bestMatch = null;
            if (potentialNewMatches.Count > 0)
                bestMatch = potentialNewMatches.First();

            if (bestMatch != null)
            {
                matchedNewEnemies.Add(bestMatch);

                if (!Equals(currentEnemy, bestMatch))
                {
                    enemiesToReplace[currentEnemy] = bestMatch;
                    logger.LogTrace("Matched enemy with key {Key}, data changed, marked for update.", currentKey);
                }
                else
                {
                    logger.LogTrace("Matched enemy with key {Key}, data unchanged.", currentKey);
                }
            }
            else
            {
                indicesToRemove.Add(i);
                logger.LogTrace("Enemy with key {Key} at index {Index} not matched, marked for removal.", currentKey, i);
            }
        }

        var enemiesToAdd = updatedLookup
            .SelectMany(grouping => grouping) 
            .Where(updatedEnemy => !matchedNewEnemies.Contains(updatedEnemy))
            .ToList();

        logger.LogTrace("Matching complete. Identified {AddCount} additions, {RemoveCount} removals, {ReplaceCount} replacements.", enemiesToAdd.Count, indicesToRemove.Count, enemiesToReplace.Count);
        return new EnemyChangeSet(indicesToRemove, enemiesToAdd, enemiesToReplace);
    }

    /// <summary>
    /// Applies the determined changes (additions, removals, replacements) to the source ObservableList.
    /// </summary>
    private static void ApplyChanges(
        ObservableList<DecodedEnemy> sourceList,
        EnemyChangeSet changeSet,
        ILogger logger)
    {
        logger.LogTrace("Applying changes to source list.");

        if (changeSet.EnemiesToReplace.Count > 0)
        {
            for (int i = 0; i < sourceList.Count; i++)
            {
                DecodedEnemy currentEnemy = sourceList[i];
                if (changeSet.EnemiesToReplace.TryGetValue(currentEnemy, out DecodedEnemy? newData))
                {
                    sourceList[i] = newData;
                    logger.LogTrace("Applied replacement at index {Index}.", i);
                }
            }

           logger.LogTrace("Applied {Count} replacements.", changeSet.EnemiesToReplace.Count);
        }

        if (changeSet.IndicesToRemove.Count > 0)
        {
            foreach (int index in changeSet.IndicesToRemove)
                sourceList.RemoveAt(index);
           logger.LogTrace("Removed {Count} enemies.", changeSet.IndicesToRemove.Count);
        }

        if (changeSet.EnemiesToAdd.Count > 0)
        {
            sourceList.AddRange(changeSet.EnemiesToAdd);
            logger.LogTrace("Added {Count} enemies.", changeSet.EnemiesToAdd.Count);
        }

       logger.LogTrace("Finished applying changes. Total items in source list: {Total}", sourceList.Count);
    }

    public void Dispose()
    {
       _logger.LogInformation("Disposing InGameEnemiesViewModel");

        _subscription.Dispose();
        EnemiesView.Dispose();

       _logger.LogInformation("InGameEnemiesViewModel disposed.");
        GC.SuppressFinalize(this);
    }

    private sealed record EnemyChangeSet(
        List<int> IndicesToRemove,
        List<DecodedEnemy> EnemiesToAdd,
        Dictionary<DecodedEnemy, DecodedEnemy> EnemiesToReplace
    );
}

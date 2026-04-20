using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;

internal sealed class EnemyDiffPlanner
{
    private static readonly TimeSpan DeadEnemyDisplayDuration = TimeSpan.FromSeconds(3);

    private readonly Dictionary<Ulid, DateTimeOffset> _enemyDeathTimes = [];
    private readonly Dictionary<Ulid, DateTimeOffset> _pendingRemovals = [];

    public EnemyListUpdatePlan BuildPlan(CollectionDiff<DecodedEnemy> diff, string scenarioName, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(diff);

        List<InGameEnemyViewModel> newViewModels = [];
        List<EntityChange<DecodedEnemy>> updatedEnemies = [];
        HashSet<Ulid> removedIds = [];

        CollectChangedEnemyEffects(diff.Changed, scenarioName, now, newViewModels, updatedEnemies, removedIds);
        CollectAddedEnemyViewModels(diff.Added, scenarioName, now, newViewModels);
        ClassifyRemovedEnemies(diff.Removed, now, removedIds);
        ExpirePendingRemovals(now, removedIds);
        ExpireDeadEnemies(now, removedIds);

        return new EnemyListUpdatePlan(scenarioName, [.. removedIds], updatedEnemies, newViewModels);
    }

    public void Clear()
    {
        _enemyDeathTimes.Clear();
        _pendingRemovals.Clear();
    }

    private void CollectChangedEnemyEffects(
        IReadOnlyList<EntityChange<DecodedEnemy>> changedEnemies,
        string scenarioName,
        DateTimeOffset now,
        List<InGameEnemyViewModel> newViewModels,
        List<EntityChange<DecodedEnemy>> updatedEnemies,
        HashSet<Ulid> removedIds
    )
    {
        foreach (EntityChange<DecodedEnemy> change in changedEnemies)
        {
            bool wasValid = IsEnemyBasicallyValid(change.Previous);
            bool isValid = IsEnemyBasicallyValid(change.Current);

            if (!wasValid && isValid)
            {
                TrackNewEnemy(change.Current, now);
                newViewModels.Add(new InGameEnemyViewModel(change.Current, scenarioName));
                continue;
            }

            if (wasValid && !isValid)
            {
                removedIds.Add(change.Current.Id);
                _enemyDeathTimes.Remove(change.Current.Id);
                _pendingRemovals.Remove(change.Current.Id);
                continue;
            }

            if (!isValid)
                continue;

            UpdateDeathTracking(change.Previous, change.Current, now);
            updatedEnemies.Add(change);
        }
    }

    private void CollectAddedEnemyViewModels(
        IReadOnlyList<DecodedEnemy> addedEnemies,
        string scenarioName,
        DateTimeOffset now,
        List<InGameEnemyViewModel> newViewModels
    )
    {
        foreach (DecodedEnemy enemy in addedEnemies)
        {
            if (!IsEnemyBasicallyValid(enemy))
                continue;

            TrackNewEnemy(enemy, now);
            newViewModels.Add(new InGameEnemyViewModel(enemy, scenarioName));
        }
    }

    private void ClassifyRemovedEnemies(
        IReadOnlyList<DecodedEnemy> removedEnemies,
        DateTimeOffset now,
        HashSet<Ulid> removedIds
    )
    {
        foreach (DecodedEnemy enemy in removedEnemies)
        {
            if (
                _enemyDeathTimes.TryGetValue(enemy.Id, out DateTimeOffset deathTime)
                && (now - deathTime) < DeadEnemyDisplayDuration
            )
            {
                _pendingRemovals[enemy.Id] = deathTime + DeadEnemyDisplayDuration;
            }
            else
            {
                removedIds.Add(enemy.Id);
                _pendingRemovals.Remove(enemy.Id);
            }

            _enemyDeathTimes.Remove(enemy.Id);
        }
    }

    private void ExpirePendingRemovals(DateTimeOffset now, HashSet<Ulid> removedIds)
    {
        List<Ulid>? expiredIds = null;

        foreach ((Ulid id, DateTimeOffset expiryTime) in _pendingRemovals)
        {
            if (now < expiryTime)
                continue;

            expiredIds ??= [];
            expiredIds.Add(id);
            removedIds.Add(id);
        }

        if (expiredIds is null)
            return;

        foreach (Ulid id in expiredIds)
            _pendingRemovals.Remove(id);
    }

    private void ExpireDeadEnemies(DateTimeOffset now, HashSet<Ulid> removedIds)
    {
        List<Ulid>? expiredIds = null;

        foreach ((Ulid id, DateTimeOffset deathTime) in _enemyDeathTimes)
        {
            if ((now - deathTime) < DeadEnemyDisplayDuration)
                continue;

            expiredIds ??= [];
            expiredIds.Add(id);
            removedIds.Add(id);
        }

        if (expiredIds is null)
            return;

        foreach (Ulid id in expiredIds)
        {
            _enemyDeathTimes.Remove(id);
            _pendingRemovals.Remove(id);
        }
    }

    private void TrackNewEnemy(DecodedEnemy enemy, DateTimeOffset now)
    {
        if (IsDead(enemy))
            _enemyDeathTimes.TryAdd(enemy.Id, now);
        else
            _pendingRemovals.Remove(enemy.Id);
    }

    private void UpdateDeathTracking(DecodedEnemy previousEnemy, DecodedEnemy currentEnemy, DateTimeOffset now)
    {
        bool wasDead = IsDead(previousEnemy);
        bool isDead = IsDead(currentEnemy);

        if (isDead && !wasDead)
        {
            _enemyDeathTimes[currentEnemy.Id] = now;
            return;
        }

        if (isDead)
            return;

        _enemyDeathTimes.Remove(currentEnemy.Id);
        _pendingRemovals.Remove(currentEnemy.Id);
    }

    private static bool IsDead(DecodedEnemy enemy) =>
        EnemyStatusUtility.IsDeadStatus(
            EnemyStatusUtility.GetHealthStatusForFileTwo(
                enemy.SlotId,
                enemy.NameId,
                enemy.CurHp,
                enemy.MaxHp,
                enemy.Name
            )
        );

    private static bool IsEnemyBasicallyValid(DecodedEnemy enemy) =>
        !string.IsNullOrEmpty(enemy.Name) && enemy.RoomId != 0 && enemy is { SlotId: > 0, MaxHp: > 0 };
}

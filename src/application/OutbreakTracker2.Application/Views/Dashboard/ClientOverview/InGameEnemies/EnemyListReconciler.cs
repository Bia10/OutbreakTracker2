using ObservableCollections;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;

internal static class EnemyListReconciler
{
    public static void Apply(
        EnemyListUpdatePlan plan,
        ObservableList<InGameEnemyViewModel> enemies,
        IDictionary<Ulid, InGameEnemyViewModel> viewModelCache
    )
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(viewModelCache);

        foreach (Ulid id in plan.RemovedIds)
        {
            if (viewModelCache.Remove(id, out InGameEnemyViewModel? viewModel))
                enemies.Remove(viewModel);
        }

        foreach (EntityChange<DecodedEnemy> change in plan.UpdatedEnemies)
        {
            if (viewModelCache.TryGetValue(change.Current.Id, out InGameEnemyViewModel? viewModel))
                viewModel.Update(change.Current, plan.ScenarioName);
        }

        if (plan.NewViewModels.Count == 0)
            return;

        foreach (InGameEnemyViewModel viewModel in plan.NewViewModels)
            viewModelCache[viewModel.UniqueId] = viewModel;

        enemies.AddRange(plan.NewViewModels);
    }
}

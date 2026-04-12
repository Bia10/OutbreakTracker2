using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;

internal sealed record EnemyListUpdatePlan(
    string ScenarioName,
    IReadOnlyList<Ulid> RemovedIds,
    IReadOnlyList<EntityChange<DecodedEnemy>> UpdatedEnemies,
    IReadOnlyList<InGameEnemyViewModel> NewViewModels
)
{
    public bool HasChanges => RemovedIds.Count > 0 || UpdatedEnemies.Count > 0 || NewViewModels.Count > 0;
}

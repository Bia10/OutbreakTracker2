using OutbreakTracker2.Application.Services.Data;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal sealed class EnemyAlertRulesProvider(IAppSettingsService settingsService, ICurrentScenarioState scenarioState)
    : IAlertRuleProvider<DecodedEnemy>
{
    private readonly IAppSettingsService _settingsService = settingsService;
    private readonly ICurrentScenarioState _scenarioState = scenarioState;

    public void Register(IEntityTracker<DecodedEnemy> tracker) =>
        EnemyAlertRules.Register(tracker, _settingsService, _scenarioState);
}

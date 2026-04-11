using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileOne;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.FileTwo;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;

/// <summary>
/// Owns the 9 scenario-specific sub-ViewModels and routes DecodedInGameScenario
/// updates to the correct one. All sub-ViewModels are parameterless and live for
/// the lifetime of this router (same as InGameScenarioViewModel).
/// </summary>
internal sealed class ScenarioViewModelRouter
{
    private readonly Dictionary<Scenario, (ObservableObject Vm, Action<DecodedInGameScenario> Update)> _map;

    public ScenarioViewModelRouter()
    {
        DesperateTimesViewModel desperateTimesVm = new();
        EndOfTheRoadViewModel endOfTheRoadVm = new();
        FlashbackViewModel flashbackVm = new();
        UnderbellyViewModel underbellyVm = new();
        WildThingsViewModel wildThingsVm = new();
        HellfireViewModel hellfireVm = new();
        TheHiveViewModel theHiveVm = new();
        DecisionsDecisionsViewModel decisionsDecisionsVm = new();
        BelowFreezingPointViewModel belowFreezingPointVm = new();

        _map = new Dictionary<Scenario, (ObservableObject, Action<DecodedInGameScenario>)>
        {
            { Scenario.DesperateTimes, (desperateTimesVm, s => desperateTimesVm.Update(s)) },
            { Scenario.EndOfTheRoad, (endOfTheRoadVm, s => endOfTheRoadVm.Update(s)) },
            { Scenario.Underbelly, (underbellyVm, s => underbellyVm.Update(s)) },
            { Scenario.WildThings, (wildThingsVm, s => wildThingsVm.Update(s)) },
            { Scenario.Hellfire, (hellfireVm, s => hellfireVm.Update(s)) },
            { Scenario.TheHive, (theHiveVm, s => theHiveVm.Update(s)) },
            { Scenario.DecisionsDecisions, (decisionsDecisionsVm, s => decisionsDecisionsVm.Update(s)) },
            { Scenario.BelowFreezingPoint, (belowFreezingPointVm, s => belowFreezingPointVm.Update(s)) },
            { Scenario.Flashback, (flashbackVm, s => flashbackVm.Update(s)) },
        };
    }

    /// <summary>
    /// Updates the sub-ViewModel for the current scenario and returns it,
    /// or returns <c>null</c> if the scenario name cannot be parsed.
    /// </summary>
    public ObservableObject? Route(DecodedInGameScenario scenario, ILogger logger)
    {
        if (
            string.IsNullOrEmpty(scenario.ScenarioName)
            || scenario.ScenarioName.Equals("Unknown(0)", StringComparison.Ordinal)
        )
            return null;

        if (!EnumUtility.TryParseByValueOrMember(scenario.ScenarioName, out Scenario scenarioType))
        {
            logger.LogWarning("Failed to parse scenario name: {ScenarioName}", scenario.ScenarioName);
            return null;
        }

        if (!_map.TryGetValue(scenarioType, out (ObservableObject Vm, Action<DecodedInGameScenario> Update) entry))
        {
            logger.LogTrace("No specific view configured for scenario: {ScenarioName}", scenario.ScenarioName);
            return null;
        }

        entry.Update(scenario);
        return entry.Vm;
    }
}

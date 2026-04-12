using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;

/// <summary>
/// Routes <see cref="DecodedInGameScenario"/> updates to the DI-registered
/// scenario-specific view model for the active scenario.
/// </summary>
public sealed class ScenarioViewModelRouter
{
    private readonly Dictionary<Scenario, IScenarioSpecificViewModel> _map;

    internal ScenarioViewModelRouter(IEnumerable<IScenarioSpecificViewModel> scenarioViewModels)
    {
        ArgumentNullException.ThrowIfNull(scenarioViewModels);

        _map = scenarioViewModels.ToDictionary(viewModel => viewModel.ScenarioType);
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

        if (!_map.TryGetValue(scenarioType, out IScenarioSpecificViewModel? entry))
        {
            logger.LogTrace("No specific view configured for scenario: {ScenarioName}", scenario.ScenarioName);
            return null;
        }

        if (entry is not ObservableObject viewModel)
        {
            logger.LogError(
                "Scenario-specific view model {ViewModelType} does not derive from ObservableObject",
                entry.GetType().Name
            );
            return null;
        }

        entry.Update(scenario);
        return viewModel;
    }
}

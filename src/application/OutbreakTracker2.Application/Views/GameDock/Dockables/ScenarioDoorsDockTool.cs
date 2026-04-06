using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entitites;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Floating dock tool that shows the in-game door list for the active scenario.
/// Opened on demand via the "Doors" button in the scenario info panel.
/// </summary>
public sealed class ScenarioDoorsDockTool(ScenarioEntitiesViewModel scenarioEntitiesViewModel) : Tool
{
    public ScenarioEntitiesViewModel ScenarioEntitiesViewModel { get; } = scenarioEntitiesViewModel;
}

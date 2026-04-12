using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Floating dock tool that shows the in-game enemy list for the active scenario.
/// Opened on demand via the "Enemies" button in the scenario info panel.
/// </summary>
public sealed class ScenarioEnemiesDockTool(ScenarioEntitiesViewModel scenarioEntitiesViewModel) : Tool
{
    public ScenarioEntitiesViewModel ScenarioEntitiesViewModel { get; } = scenarioEntitiesViewModel;
}

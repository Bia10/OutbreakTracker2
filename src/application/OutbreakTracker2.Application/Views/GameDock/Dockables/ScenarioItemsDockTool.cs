using Dock.Model.Mvvm.Controls;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Floating dock tool that shows the in-game item list for the active scenario.
/// Opened on demand via the "Items" button in the scenario info panel.
/// </summary>
public sealed class ScenarioItemsDockTool(ScenarioItemsDockViewModel scenarioItemsDockViewModel) : Tool
{
    public ScenarioItemsDockViewModel ScenarioItemsDockViewModel { get; } = scenarioItemsDockViewModel;
}

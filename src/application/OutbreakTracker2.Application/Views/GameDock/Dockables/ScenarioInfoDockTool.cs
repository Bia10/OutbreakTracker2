using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Dock tool panel that shows current scenario / in-game state information.
/// Docked at the bottom of the right side panel.
/// </summary>
public sealed class ScenarioInfoDockTool(InGameScenarioViewModel inGameScenarioViewModel) : Tool
{
    public InGameScenarioViewModel InGameScenarioViewModel { get; } = inGameScenarioViewModel;
}

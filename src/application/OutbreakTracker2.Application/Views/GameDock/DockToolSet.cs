using OutbreakTracker2.Application.Views.GameDock.Dockables;

namespace OutbreakTracker2.Application.Views.GameDock;

/// <summary>
/// Groups the 8 dock tool instances that <see cref="GameDockFactory"/> arranges into a layout.
/// Registered as a singleton; GameDockFactory receives it as a single constructor parameter.
/// </summary>
public sealed record DockToolSet(
    GameScreenTool GameScreen,
    EntitiesDockTool Entities,
    MapDockTool Map,
    PlayersDockTool Players,
    ScenarioInfoDockTool ScenarioInfo,
    ScenarioItemsDockTool ScenarioItems,
    ScenarioEnemiesDockTool ScenarioEnemies,
    ScenarioDoorsDockTool ScenarioDoors
);

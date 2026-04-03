using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.GameDock.Dockables;

namespace OutbreakTracker2.Application.Views.GameDock;

/// <summary>
/// Creates and initialises the docking layout for the Game Dock page.
/// <para>
/// Layout (horizontal proportional split):
/// <code>
/// ┌────────────┬────────────────────────────┬────────────────┐
/// │  Mob List  │   Game Screen (Document)   │  Players (top) │
/// │  (left     │        [22%  ←  fill  →    │  ─────────────  │
/// │   22%,top) │           56%)             │ Scenario (bot) │
/// ├────────────┤                            │     (22%)      │
/// │    Map     │                            │                │
/// │  (left,bot)│                            │                │
/// └────────────┴────────────────────────────┴────────────────┘
/// </code>
/// </para>
/// <para>
/// The game screen document has <see cref="IDockable.CanFloat"/> and
/// <see cref="IDockable.CanDrag"/> disabled to prevent Win32 NativeControlHost
/// z-order conflicts when panels are dragged over the HWND area.
/// </para>
/// </summary>
public sealed class GameDockFactory(
    GameScreenDocument gameScreenDocument,
    EnemyListDockTool enemyListTool,
    MapDockTool mapDockTool,
    PlayersDockTool playersTool,
    ScenarioInfoDockTool scenarioInfoTool
) : Factory
{
    public override IRootDock CreateLayout()
    {
        gameScreenDocument.Id = "GameScreen";
        gameScreenDocument.Title = "Game Screen";
        gameScreenDocument.CanClose = false;
        gameScreenDocument.CanFloat = false;
        gameScreenDocument.CanDrag = false;

        enemyListTool.Id = "EnemyList";
        enemyListTool.Title = "Mob List";
        enemyListTool.CanClose = false;
        enemyListTool.CanFloat = false;

        mapDockTool.Id = "Map";
        mapDockTool.Title = "Map";
        mapDockTool.CanClose = false;
        mapDockTool.CanFloat = false;

        playersTool.Id = "Players";
        playersTool.Title = "Players";
        playersTool.CanClose = false;
        playersTool.CanFloat = false;

        scenarioInfoTool.Id = "ScenarioInfo";
        scenarioInfoTool.Title = "Scenario";
        scenarioInfoTool.CanClose = false;
        scenarioInfoTool.CanFloat = false;

        var leftDock = new ProportionalDock
        {
            Id = "LeftDock",
            Title = "Left",
            Proportion = 0.22,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>(
                new ToolDock
                {
                    Id = "EnemyListToolDock",
                    Proportion = 0.6,
                    ActiveDockable = enemyListTool,
                    VisibleDockables = CreateList<IDockable>(enemyListTool),
                    Alignment = Alignment.Left,
                    GripMode = GripMode.Visible,
                },
                new ProportionalDockSplitter(),
                new ToolDock
                {
                    Id = "MapToolDock",
                    Proportion = 0.4,
                    ActiveDockable = mapDockTool,
                    VisibleDockables = CreateList<IDockable>(mapDockTool),
                    Alignment = Alignment.Left,
                    GripMode = GripMode.Visible,
                }
            ),
        };

        var documentDock = new DocumentDock
        {
            Id = "DocumentDock",
            Title = "Game Screen",
            ActiveDockable = gameScreenDocument,
            VisibleDockables = CreateList<IDockable>(gameScreenDocument),
            CanCreateDocument = false,
        };

        var rightDock = new ProportionalDock
        {
            Id = "RightDock",
            Title = "Right",
            Proportion = 0.22,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>(
                new ToolDock
                {
                    Id = "PlayersToolDock",
                    Proportion = 0.6,
                    ActiveDockable = playersTool,
                    VisibleDockables = CreateList<IDockable>(playersTool),
                    Alignment = Alignment.Right,
                    GripMode = GripMode.Visible,
                },
                new ProportionalDockSplitter(),
                new ToolDock
                {
                    Id = "ScenarioToolDock",
                    Proportion = 0.4,
                    ActiveDockable = scenarioInfoTool,
                    VisibleDockables = CreateList<IDockable>(scenarioInfoTool),
                    Alignment = Alignment.Right,
                    GripMode = GripMode.Visible,
                }
            ),
        };

        var mainLayout = new ProportionalDock
        {
            Id = "MainLayout",
            Title = "Main",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                leftDock,
                new ProportionalDockSplitter(),
                documentDock,
                new ProportionalDockSplitter(),
                rightDock
            ),
        };

        var rootDock = CreateRootDock();
        rootDock.Id = "Root";
        rootDock.IsCollapsable = false;
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        return rootDock;
    }

    public override void InitLayout(IDockable layout)
    {
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>(StringComparer.Ordinal)
        {
            [nameof(IDockWindow)] = static () => new HostWindow(),
        };

        base.InitLayout(layout);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using Material.Icons;
using OutbreakTracker2.Application.Pages;
using OutbreakTracker2.Application.Views.GameDock.Dockables;

namespace OutbreakTracker2.Application.Views.GameDock;

/// <summary>
/// Page view model for the "Game Dock" side menu entry.
/// Holds the Dock layout (<see cref="IRootDock"/>) that puts the embedded
/// game window dead-centre with the mob list docked on the left and
/// player / scenario panels on the right.
/// </summary>
public sealed partial class GameDockViewModel : PageBase
{
    [ObservableProperty]
    private IRootDock? _layout;

    public GameDockViewModel(
        GameDockFactory factory,
        MapDockTool mapDockTool,
        ScenarioInfoDockTool scenarioInfoTool,
        ScenarioItemsDockTool scenarioItemsTool,
        ScenarioEnemiesDockTool scenarioEnemiesDockTool,
        ScenarioDoorsDockTool scenarioDoorsDockTool,
        ScenarioEntityCommands entityCommands
    )
        : base("Game Dock", MaterialIconKind.Gamepad, 300)
    {
        Layout = factory.CreateLayout();
        factory.InitLayout(Layout);

        // After InitLayout, Owner is set on every dockable.
        // Use the scenario info tool's parent dock as the home for entity tools
        // when they need to be re-added after the user closes their floating window.
        IDock homeDock = (IDock)scenarioInfoTool.Owner!;

        entityCommands.ShowItems = new RelayCommand(() =>
        {
            EnsureOriginalOwner(scenarioItemsTool, homeDock);
            if (scenarioItemsTool.Owner is null)
                factory.AddDockable(homeDock, scenarioItemsTool);
            factory.FloatDockable(scenarioItemsTool);
        });

        entityCommands.ShowEnemies = new RelayCommand(() =>
        {
            EnsureOriginalOwner(scenarioEnemiesDockTool, homeDock);
            if (scenarioEnemiesDockTool.Owner is null)
                factory.AddDockable(homeDock, scenarioEnemiesDockTool);
            factory.FloatDockable(scenarioEnemiesDockTool);
        });

        entityCommands.ShowDoors = new RelayCommand(() =>
        {
            EnsureOriginalOwner(scenarioDoorsDockTool, homeDock);
            if (scenarioDoorsDockTool.Owner is null)
                factory.AddDockable(homeDock, scenarioDoorsDockTool);
            factory.FloatDockable(scenarioDoorsDockTool);
        });

        entityCommands.ShowMap = new RelayCommand(() =>
        {
            EnsureOriginalOwner(mapDockTool, homeDock);
            if (mapDockTool.Owner is null)
                factory.AddDockable(homeDock, mapDockTool);
            factory.FloatDockable(mapDockTool);
        });
    }

    private static void EnsureOriginalOwner(IDockable dockable, IDock owner)
    {
        dockable.OriginalOwner ??= owner;
    }
}

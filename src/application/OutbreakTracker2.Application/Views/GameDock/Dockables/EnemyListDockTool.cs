using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Dock tool panel that displays the in-game enemy / mob list.
/// Docked on the left side of the game screen.
/// </summary>
public sealed class EnemyListDockTool(InGameEnemiesViewModel inGameEnemiesViewModel) : Tool
{
    public InGameEnemiesViewModel InGameEnemiesViewModel { get; } = inGameEnemiesViewModel;
}

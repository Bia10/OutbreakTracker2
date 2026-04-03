using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGamePlayers;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Dock tool panel that displays live in-game player stats.
/// Docked at the top of the right side panel.
/// </summary>
public sealed class PlayersDockTool(InGamePlayersViewModel inGamePlayersViewModel) : Tool
{
    public InGamePlayersViewModel InGamePlayersViewModel { get; } = inGamePlayersViewModel;
}

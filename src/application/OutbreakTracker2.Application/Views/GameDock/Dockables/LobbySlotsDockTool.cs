using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbySlots;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Dock tool panel that shows the lobby slot list.
/// Docked at the bottom of the left side panel, beneath the mob list.
/// </summary>
public sealed class LobbySlotsDockTool(LobbySlotsViewModel lobbySlotsViewModel) : Tool
{
    public LobbySlotsViewModel LobbySlotsViewModel { get; } = lobbySlotsViewModel;
}

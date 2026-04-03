using Dock.Model.Mvvm.Controls;
using OutbreakTracker2.Application.Views.Map.Canvas;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Dock tool panel that renders the mini-map canvas showing entity positions.
/// Docked at the bottom of the left side panel, beneath the mob list.
/// </summary>
public sealed class MapDockTool(MapCanvasViewModel mapCanvasViewModel) : Tool
{
    public MapCanvasViewModel MapCanvasViewModel { get; } = mapCanvasViewModel;
}

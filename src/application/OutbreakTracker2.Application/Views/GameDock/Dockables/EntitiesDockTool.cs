using Dock.Model.Mvvm.Controls;

namespace OutbreakTracker2.Application.Views.GameDock.Dockables;

/// <summary>
/// Dock tool panel that displays all in-game scenario entities
/// (enemies, mines, gas canisters, etc.). Docked on the left side of the game screen.
/// </summary>
public sealed class EntitiesDockTool(EntitiesDockViewModel entitiesDockViewModel) : Tool
{
    public EntitiesDockViewModel EntitiesDockViewModel { get; } = entitiesDockViewModel;
}

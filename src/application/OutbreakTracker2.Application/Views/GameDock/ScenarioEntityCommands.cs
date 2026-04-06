using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace OutbreakTracker2.Application.Views.GameDock;

/// <summary>
/// Holds the commands that open the floating entity-detail dock windows
/// (Items, Enemies, Doors) from inside the scenario info panel.
/// The commands are late-bound: they start as no-ops and are configured
/// by <see cref="GameDockViewModel"/> after the dock layout is initialised.
/// </summary>
public sealed class ScenarioEntityCommands
{
    public ICommand ShowItems { get; set; } = new RelayCommand(static () => { });
    public ICommand ShowEnemies { get; set; } = new RelayCommand(static () => { });
    public ICommand ShowDoors { get; set; } = new RelayCommand(static () => { });
}

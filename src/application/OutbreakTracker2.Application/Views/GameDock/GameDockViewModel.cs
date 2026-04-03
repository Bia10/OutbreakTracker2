using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
using Material.Icons;
using OutbreakTracker2.Application.Pages;

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

    public GameDockViewModel(GameDockFactory factory)
        : base("Game Dock", MaterialIconKind.Gamepad, 300)
    {
        Layout = factory.CreateLayout();
        factory.InitLayout(Layout);
    }
}

using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace OutbreakTracker2.Application.Views.GameDock;

/// <summary>
/// Holds the commands that open the floating entity-detail dock windows
/// (Items, Enemies, Doors) from inside the scenario info panel.
/// The commands are configured by <see cref="GameDockViewModel"/> once the
/// dock layout is initialised. Until then they remain disabled.
/// </summary>
public sealed class ScenarioEntityCommands
{
    private readonly RelayCommand _showItemsCommand;
    private readonly RelayCommand _showEnemiesCommand;
    private readonly RelayCommand _showDoorsCommand;
    private readonly RelayCommand _showMapCommand;

    private Action? _showItemsAction;
    private Action? _showEnemiesAction;
    private Action? _showDoorsAction;
    private Action? _showMapAction;

    public ScenarioEntityCommands()
    {
        _showItemsCommand = new RelayCommand(() => _showItemsAction?.Invoke(), () => _showItemsAction is not null);
        _showEnemiesCommand = new RelayCommand(
            () => _showEnemiesAction?.Invoke(),
            () => _showEnemiesAction is not null
        );
        _showDoorsCommand = new RelayCommand(() => _showDoorsAction?.Invoke(), () => _showDoorsAction is not null);
        _showMapCommand = new RelayCommand(() => _showMapAction?.Invoke(), () => _showMapAction is not null);
    }

    public ICommand ShowItems => _showItemsCommand;
    public ICommand ShowEnemies => _showEnemiesCommand;
    public ICommand ShowDoors => _showDoorsCommand;
    public ICommand ShowMap => _showMapCommand;

    public void Configure(Action showItems, Action showEnemies, Action showDoors, Action showMap)
    {
        ArgumentNullException.ThrowIfNull(showItems);
        ArgumentNullException.ThrowIfNull(showEnemies);
        ArgumentNullException.ThrowIfNull(showDoors);
        ArgumentNullException.ThrowIfNull(showMap);

        _showItemsAction = showItems;
        _showEnemiesAction = showEnemies;
        _showDoorsAction = showDoors;
        _showMapAction = showMap;

        _showItemsCommand.NotifyCanExecuteChanged();
        _showEnemiesCommand.NotifyCanExecuteChanged();
        _showDoorsCommand.NotifyCanExecuteChanged();
        _showMapCommand.NotifyCanExecuteChanged();
    }
}

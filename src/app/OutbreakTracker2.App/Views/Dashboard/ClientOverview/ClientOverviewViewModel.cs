using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Debug;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGameEnemies;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayers;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview;

public class ClientOverviewViewModel : ObservableObject
{
    public DebugViewModel DebugViewModel { get; }

    public InGamePlayersViewModel InGamePlayersViewModel { get; }

    public InGameEnemiesViewModel InGameEnemiesViewModel { get; }

    public ClientOverviewViewModel(
        DebugViewModel debugViewModel,
        InGamePlayersViewModel inGamePlayersViewModel,
        InGameEnemiesViewModel inGameEnemiesViewModel)
    {
        DebugViewModel =
            debugViewModel ?? throw new ArgumentNullException(nameof(debugViewModel));
        InGamePlayersViewModel =
            inGamePlayersViewModel ?? throw new ArgumentNullException(nameof(inGamePlayersViewModel));
        InGameEnemiesViewModel =
            inGameEnemiesViewModel ?? throw new ArgumentNullException(nameof(inGameEnemiesViewModel));
    }
}

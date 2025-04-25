using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Debug;
using System;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.InGamePlayers;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview;

public class ClientOverviewViewModel : ObservableObject
{
    public DebugViewModel DebugViewModel { get; }

    public InGamePlayersViewModel InGamePlayersViewModel { get; }

    public ClientOverviewViewModel(DebugViewModel debugViewModel, InGamePlayersViewModel inGamePlayersViewModel)
    {
        DebugViewModel = debugViewModel ?? throw new ArgumentNullException(nameof(debugViewModel));
        InGamePlayersViewModel = inGamePlayersViewModel ?? throw new ArgumentNullException(nameof(inGamePlayersViewModel));
    }
}

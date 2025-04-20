using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Views.Dashboard.ClientOverview.Debug;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview;

public class ClientOverviewViewModel : ObservableObject
{
    public DebugViewModel DebugViewModel { get; }

    public ClientOverviewViewModel(DebugViewModel debugViewModel)
    {
        DebugViewModel = debugViewModel ?? throw new ArgumentNullException(nameof(debugViewModel));
    }
}

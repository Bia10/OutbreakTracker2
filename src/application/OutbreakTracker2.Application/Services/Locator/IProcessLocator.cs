using System;
using System.Collections.Generic;
using System.Diagnostics;
using R3;

namespace OutbreakTracker2.Application.Services.Locator;

public interface IProcessLocator
{
    Observable<bool> IsProcessRunningPolling(string processName, TimeSpan? checkInterval = null);

    Observable<bool> IsProcessRunningEventDriven(
        string processName,
        TimeSpan? checkInterval = null
    );

    IReadOnlyList<int> GetProcessIds(string processName);

    IEnumerable<Process> GetProcessesByName(string processName);
}

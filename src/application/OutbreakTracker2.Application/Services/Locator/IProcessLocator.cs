using R3;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OutbreakTracker2.Application.Services.Locator;

public interface IProcessLocator
{
    Observable<bool> IsProcessRunningPolling(string processName, TimeSpan? checkInterval = null);

    Observable<bool> IsProcessRunningEventDriven(string processName, TimeSpan? checkInterval = null);

    List<int> GetProcessIds(string processName);

    IEnumerable<Process> GetProcessesByName(string processName);
}

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using R3;

namespace OutbreakTracker2.Application.Services.Locator;

public sealed class ProcessLocator(ILogger<ProcessLocator> logger) : IProcessLocator
{
    private readonly ILogger<ProcessLocator> _logger = logger;

    // Polling-based implementation
    public Observable<bool> IsProcessRunningPolling(string processName, TimeSpan? checkInterval = null)
    {
        return Observable
            .Timer(TimeSpan.Zero, checkInterval ?? TimeSpan.FromSeconds(1))
            .Select(_ =>
            {
                try
                {
                    return Process.GetProcessesByName(processName).Length is not 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to poll process state for {ProcessName}", processName);
                    return false;
                }
            })
            .DistinctUntilChanged();
    }

    // WMI event-driven implementation removed — WMI uses COM interop which is not supported in
    // NativeAOT. Process detection now uses polling exclusively via IsProcessRunningPolling.

    public IReadOnlyList<int> GetProcessIds(string processName)
    {
        try
        {
            return [.. GetProcessesByName(processName).Select(p => p.Id)];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate process ids for {ProcessName}", processName);
            return [];
        }
    }

    public IEnumerable<Process> GetProcessesByName(string processName) => Process.GetProcessesByName(processName);
}

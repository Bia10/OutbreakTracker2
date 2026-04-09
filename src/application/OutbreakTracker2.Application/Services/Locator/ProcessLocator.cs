using System.Diagnostics;
using System.Runtime.Versioning;
using R3;
#if WINDOWS
using System.Management;
#endif

namespace OutbreakTracker2.Application.Services.Locator;

public sealed class ProcessLocator : IProcessLocator
{
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
                    Debug.WriteLine($"Polling error: {ex}");
                    return false;
                }
            })
            .DistinctUntilChanged();
    }

    // WMI event-driven implementation (Windows only)
    [SupportedOSPlatform("windows")]
    public Observable<bool> IsProcessRunningEventDriven(string processName, TimeSpan? checkInterval = null)
    {
#if WINDOWS
        return Observable
            .Create<bool>(observer =>
            {
                if (Environment.OSVersion.Platform is not PlatformID.Win32NT)
                {
                    observer.OnCompleted(Result.Failure(new NotSupportedException("WMI is only supported on Windows")));
                    return Disposable.Empty;
                }

                try
                {
                    WqlEventQuery query = new()
                    {
                        EventClassName = "__InstanceCreationEvent",
                        WithinInterval = checkInterval ?? TimeSpan.FromSeconds(1),
                        Condition = $"TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{processName}.exe'",
                    };

                    ManagementEventWatcher watcher = new(query);
                    bool initialCheck = Process.GetProcessesByName(processName).Length is not 0;

                    // Send initial state
                    observer.OnNext(initialCheck);

                    watcher.EventArrived += EventHandler;
                    watcher.Start();

                    return Disposable.Create(() =>
                    {
                        watcher.EventArrived -= EventHandler;
                        watcher.Stop();
                        watcher.Dispose();
                    });

                    void EventHandler(object sender, EventArrivedEventArgs args)
                    {
                        try
                        {
                            bool isRunning = Process.GetProcessesByName(processName).Length is not 0;
                            observer.OnNext(isRunning);
                        }
                        catch (Exception ex)
                        {
                            observer.OnErrorResume(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    observer.OnCompleted(Result.Failure(ex));
                    return Disposable.Empty;
                }
            })
            .DistinctUntilChanged()
            .Catch<bool, Exception>(ex =>
            {
                Debug.WriteLine($"WMI error: {ex.Message}");
                return Observable.Return(value: false);
            });
#else
        // On non-Windows platforms (no WMI), fall back to polling.
        return IsProcessRunningPolling(processName, checkInterval);
#endif
    }

    public IReadOnlyList<int> GetProcessIds(string processName)
    {
        try
        {
            return [.. GetProcessesByName(processName).Select(p => p.Id)];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetProcessIds error: {ex}");
            return [];
        }
    }

    public IEnumerable<Process> GetProcessesByName(string processName) => Process.GetProcessesByName(processName);
}

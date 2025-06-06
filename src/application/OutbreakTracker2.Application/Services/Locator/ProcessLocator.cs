using R3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;

namespace OutbreakTracker2.Application.Services.Locator;

public class ProcessLocator : IProcessLocator
{
    // Polling-based implementation
    public Observable<bool> IsProcessRunningPolling(string processName, TimeSpan? checkInterval = null)
    {
        return Observable.Timer(TimeSpan.Zero, checkInterval ?? TimeSpan.FromSeconds(1))
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
        return Observable.Create<bool>(observer =>
            {
                if (Environment.OSVersion.Platform is not PlatformID.Win32NT)
                {
                    observer.OnCompleted(new NotSupportedException("WMI is only supported on Windows"));
                    return Disposable.Empty;
                }

                try
                {
                    WqlEventQuery query = new()
                    {
                        EventClassName = "__InstanceCreationEvent",
                        WithinInterval = checkInterval ?? TimeSpan.FromSeconds(1),
                        Condition = $"TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{processName}.exe'"
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
                    observer.OnCompleted(ex);
                    return Disposable.Empty;
                }
            })
            .DistinctUntilChanged()
            .Catch<bool, Exception>(ex =>
            {
                Debug.WriteLine($"WMI error: {ex.Message}");
                return Observable.Return(false);
            });
    }

    public List<int> GetProcessIds(string processName)
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

    public IEnumerable<Process> GetProcessesByName(string processName)
        => Process.GetProcessesByName(processName);
}

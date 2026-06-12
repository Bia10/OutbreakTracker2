using System.ComponentModel;
using System.Diagnostics;

namespace OutbreakTracker2.Extensions;

public static class ProcessExtensions
{
    public static int GetSafeId(this Process? process)
    {
        if (process is null)
        {
            return -1;
        }

        try
        {
            return process.Id;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return -1;
        }
    }

    public static string? GetSafeName(this Process? process)
    {
        if (process is null)
        {
            return null;
        }

        try
        {
            return process.ProcessName;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return null;
        }
    }

    public static DateTime GetSafeStartTime(this Process? process)
    {
        if (process is null)
        {
            return DateTime.MinValue;
        }

        try
        {
            return process.StartTime;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return DateTime.MinValue;
        }
    }

    public static int GetSafeExitCode(this Process? process)
    {
        if (process is null)
        {
            return -1;
        }

        try
        {
            return process.ExitCode;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return -1;
        }
    }

    public static IReadOnlyList<int> GetSafeThreadIds(this Process? process)
    {
        if (process is null)
        {
            return [];
        }

        try
        {
            int[] threadIds = new int[process.Threads.Count];
            for (int i = 0; i < process.Threads.Count; i++)
            {
                threadIds[i] = process.Threads[i].Id;
            }

            return threadIds;
        }
        catch (Exception ex)
            when (ex
                    is InvalidOperationException
                        or Win32Exception
                        or NotSupportedException
                        or PlatformNotSupportedException
            )
        {
            return [];
        }
    }
}

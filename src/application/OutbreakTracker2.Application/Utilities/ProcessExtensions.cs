using System.ComponentModel;
using System.Diagnostics;

namespace OutbreakTracker2.Application.Utilities;

internal static class ProcessExtensions
{
    /// <summary>
    /// Returns <see cref="Process.StartTime"/> or <see cref="DateTime.MinValue"/> when the start
    /// time is unavailable (e.g. the process has exited or the caller lacks permission).
    /// </summary>
    internal static DateTime GetSafeStartTime(this Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return DateTime.MinValue;
        }
    }
}

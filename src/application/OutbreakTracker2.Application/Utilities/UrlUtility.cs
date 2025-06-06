using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OutbreakTracker2.Application.Utilities;

public static class UrlUtility
{
    public static void OpenUrl(string url)
    {
        if (System.OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo(url.Replace("&", "^&")) { UseShellExecute = true });
        else if (System.OperatingSystem.IsLinux())
            Process.Start("xdg-open", url);
        else if (System.OperatingSystem.IsMacOS())
            Process.Start("open", url);
    }
}

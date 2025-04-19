using Avalonia.Controls.ApplicationLifetimes;

namespace OutbreakTracker2.App.Services;

public class ClipboardService(IClassicDesktopStyleApplicationLifetime liftime)
{
    public void CopyToClipboard(string text) => liftime.MainWindow?.Clipboard?.SetTextAsync(text);
}

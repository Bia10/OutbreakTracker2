using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Services;

public class ClipboardService(IClassicDesktopStyleApplicationLifetime lifeTime)
{
    public async Task CopyToClipboard(string text)
    {
        if (lifeTime.MainWindow?.Clipboard != null)
            await lifeTime.MainWindow.Clipboard.SetTextAsync(text).ConfigureAwait(false);
    }
}

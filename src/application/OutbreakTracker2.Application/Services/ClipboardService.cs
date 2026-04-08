using Avalonia.Controls.ApplicationLifetimes;
using Bia.LogViewer.Core;

namespace OutbreakTracker2.Application.Services;

public sealed class ClipboardService(IClassicDesktopStyleApplicationLifetime lifeTime) : IClipboardService
{
    public async Task CopyToClipboardAsync(string? text)
    {
        if (text is not null && lifeTime.MainWindow?.Clipboard != null)
            await lifeTime.MainWindow.Clipboard.SetTextAsync(text).ConfigureAwait(false);
    }
}

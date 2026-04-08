using Bia.LogViewer.Avalonia;
using Material.Icons;
using OutbreakTracker2.Application.Pages;

namespace OutbreakTracker2.Application.Views.Log;

public sealed class LogViewModel(LogViewerViewModel logViewer) : PageBase("App Log", MaterialIconKind.ScriptText, 500)
{
    public LogViewerViewModel LogViewer { get; } = logViewer;
}
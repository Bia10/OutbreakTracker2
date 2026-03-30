using Material.Icons;
using OutbreakTracker2.Application.Pages;
using OutbreakTracker2.Application.Views.Logging;

namespace OutbreakTracker2.Application.Views.Log;

public class LogViewModel(LogViewerViewModel logViewer) : PageBase("App Log", MaterialIconKind.ScriptText, 500)
{
    public LogViewerViewModel LogViewer { get; } = logViewer;
}
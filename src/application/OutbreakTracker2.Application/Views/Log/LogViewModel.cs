using Material.Icons;
using OutbreakTracker2.Application.Pages;
using OutbreakTracker2.Application.Views.Logging;

namespace OutbreakTracker2.Application.Views.Log;

public class LogViewModel : PageBase
{
    public LogViewModel(LogViewerViewModel logViewer)
        : base("App Log", MaterialIconKind.ScriptText, 500)
    {
        LogViewer = logViewer;
    }

    public LogViewerViewModel LogViewer { get; }
}

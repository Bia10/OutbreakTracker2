using Material.Icons;
using OutbreakTracker2.App.Pages;
using OutbreakTracker2.App.Views.Logging;

namespace OutbreakTracker2.App.Views.Log;

public class LogViewModel : PageBase
{
    public LogViewModel(LogViewerViewModel logViewer)
        : base("App Log", MaterialIconKind.ScriptText, 500)
    {
        LogViewer = logViewer;
    }

    public LogViewerViewModel LogViewer { get; }
}

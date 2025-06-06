using Material.Icons;
using OutbreakTracker2.App.Pages;

namespace OutbreakTracker2.App.Views.Map;

public class MapViewModel : PageBase
{
    public MapViewModel(Canvas.MapCanvasViewModel mapCanvasView)
        : base("Map", MaterialIconKind.Map, 600)
    {
        MapCanvasView = mapCanvasView;
    }

    public Canvas.MapCanvasViewModel MapCanvasView { get; }
}
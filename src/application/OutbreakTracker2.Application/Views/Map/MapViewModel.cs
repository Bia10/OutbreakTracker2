using Material.Icons;
using OutbreakTracker2.Application.Pages;
using OutbreakTracker2.Application.Views.Map.Canvas;

namespace OutbreakTracker2.Application.Views.Map;

public class MapViewModel : PageBase
{
    public MapViewModel(MapCanvasViewModel mapCanvasView)
        : base("Map", MaterialIconKind.Map, 600)
    {
        MapCanvasView = mapCanvasView;
    }

    public MapCanvasViewModel MapCanvasView { get; }
}
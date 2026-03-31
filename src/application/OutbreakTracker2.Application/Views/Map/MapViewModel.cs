using Material.Icons;
using OutbreakTracker2.Application.Pages;
using OutbreakTracker2.Application.Views.Map.Canvas;

namespace OutbreakTracker2.Application.Views.Map;

public class MapViewModel(MapCanvasViewModel mapCanvasView) : PageBase("Map", MaterialIconKind.Map, 600)
{
    public MapCanvasViewModel MapCanvasView { get; } = mapCanvasView;
}

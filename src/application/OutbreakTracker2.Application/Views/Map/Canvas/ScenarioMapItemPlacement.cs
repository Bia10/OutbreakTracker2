using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameScenario.Entities;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal sealed record ScenarioMapItemPlacement(
    ScenarioItemSlotViewModel Item,
    double CenterX,
    double CenterY,
    double Radius
);

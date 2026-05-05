namespace OutbreakTracker2.Application.Views.Map.Canvas;

public interface IPolygonCirclePackingService
{
    PolygonCirclePackingResult Pack(PolygonCirclePackingRequest request);

    ValueTask<PolygonCirclePackingResult> PackAsync(
        PolygonCirclePackingRequest request,
        CancellationToken cancellationToken = default
    );
}

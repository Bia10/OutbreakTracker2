using System.Buffers;
using Avalonia;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

public sealed class PolygonCirclePackingService : IPolygonCirclePackingService
{
    private const double MinimumSearchRadius = 0.25;
    private const double BoundaryEpsilon = 0.05;
    private const int RadiusSearchIterations = 18;
    private const int AngleSamples = 18;
    private const int PhaseSamples = 6;
    private const int CenterSearchGridSize = 10;
    private const int CenterSearchRefinementPasses = 4;
    private const int StackCandidateLimit = 16;
    private const int ParallelSearchCircleThreshold = 3;

    public PolygonCirclePackingResult Pack(PolygonCirclePackingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PackCore(request, CancellationToken.None);
    }

    public ValueTask<PolygonCirclePackingResult> PackAsync(
        PolygonCirclePackingRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<PolygonCirclePackingResult>(cancellationToken);

        if (!ShouldDispatchAsync(request.CircleCount))
            return ValueTask.FromResult(PackCore(request, cancellationToken));

        return new ValueTask<PolygonCirclePackingResult>(
            Task.Run(() => PackCore(request, cancellationToken), cancellationToken)
        );
    }

    private static PolygonCirclePackingResult PackCore(
        PolygonCirclePackingRequest request,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.CircleCount <= 0 || request.Vertices.Count < 3)
            return new PolygonCirclePackingResult([], false);

        PolygonData polygon = CreatePolygonData(request.Vertices);
        if (polygon.Area <= BoundaryEpsilon)
            return new PolygonCirclePackingResult([], false);

        PackingPoint referencePoint = EstimatePackingCenter(polygon);

        if (request.CircleCount == 1)
        {
            double radius = Math.Max(0, GetSignedDistanceToBoundary(polygon, referencePoint) - BoundaryEpsilon);
            return radius <= BoundaryEpsilon
                ? new PolygonCirclePackingResult([], false)
                : new PolygonCirclePackingResult([new PackedCircle(referencePoint.X, referencePoint.Y, radius)], true);
        }

        double upperRadiusBound = Math.Max(
            MinimumSearchRadius,
            Math.Min(polygon.Bounds.Width, polygon.Bounds.Height) / 2.0
        );

        PackingCandidate bestCandidate = PackingCandidate.Empty;
        double low = 0;
        double high = upperRadiusBound;

        for (int iteration = 0; iteration < RadiusSearchIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            double radius = (low + high) / 2.0;
            if (radius < MinimumSearchRadius)
                break;

            if (
                TryFindArrangement(
                    polygon,
                    request.CircleCount,
                    referencePoint,
                    radius,
                    cancellationToken,
                    out PackingCandidate candidate
                )
            )
            {
                bestCandidate = candidate;
                low = radius;
            }
            else
            {
                high = radius;
            }
        }

        return bestCandidate.Circles.Count == request.CircleCount
            ? new PolygonCirclePackingResult(bestCandidate.Circles, true)
            : new PolygonCirclePackingResult(bestCandidate.Circles, false);
    }

    private static bool TryFindArrangement(
        PolygonData polygon,
        int circleCount,
        PackingPoint referencePoint,
        double radius,
        CancellationToken cancellationToken,
        out PackingCandidate bestCandidate
    )
    {
        bestCandidate = PackingCandidate.Empty;
        if (radius <= BoundaryEpsilon)
            return false;

        return ShouldUseParallelSearch(circleCount)
            ? TryFindArrangementParallel(
                polygon,
                circleCount,
                referencePoint,
                radius,
                cancellationToken,
                out bestCandidate
            )
            : TryFindArrangementSequential(
                polygon,
                circleCount,
                referencePoint,
                radius,
                cancellationToken,
                out bestCandidate
            );
    }

    private static bool TryFindArrangementSequential(
        PolygonData polygon,
        int circleCount,
        PackingPoint referencePoint,
        double radius,
        CancellationToken cancellationToken,
        out PackingCandidate bestCandidate
    )
    {
        using SearchWorkerState searchState = new(circleCount);

        for (int angleIndex = 0; angleIndex < AngleSamples; angleIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SearchAngle(searchState, polygon, circleCount, referencePoint, radius, angleIndex, cancellationToken);
        }

        bestCandidate = searchState.BuildCandidate();
        return bestCandidate.Circles.Count == circleCount;
    }

    private static bool TryFindArrangementParallel(
        PolygonData polygon,
        int circleCount,
        PackingPoint referencePoint,
        double radius,
        CancellationToken cancellationToken,
        out PackingCandidate bestCandidate
    )
    {
        Lock gate = new();
        PackingCandidate sharedBest = PackingCandidate.Empty;
        ParallelOptions parallelOptions = new()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
        };

        Parallel.For<SearchWorkerState>(
            0,
            AngleSamples,
            parallelOptions,
            () => new SearchWorkerState(circleCount),
            (angleIndex, _, searchState) =>
            {
                SearchAngle(searchState, polygon, circleCount, referencePoint, radius, angleIndex, cancellationToken);
                return searchState;
            },
            searchState =>
            {
                PackingCandidate localBest = searchState.BuildCandidate();
                searchState.Dispose();

                lock (gate)
                {
                    if (sharedBest.IsBetterThan(localBest))
                        sharedBest = localBest;
                }
            }
        );

        bestCandidate = sharedBest;
        return bestCandidate.Circles.Count == circleCount;
    }

    private static void SearchAngle(
        SearchWorkerState searchState,
        PolygonData polygon,
        int circleCount,
        PackingPoint referencePoint,
        double radius,
        int angleIndex,
        CancellationToken cancellationToken
    )
    {
        double stepX = radius * 2.0;
        double stepY = Math.Sqrt(3.0) * radius;
        double angle = (angleIndex * (Math.PI / 3.0)) / AngleSamples;
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        PackingBounds localBounds = GetRotatedBounds(polygon.Vertices, referencePoint, cos, sin);
        CandidateCircle[]? rentedCandidates = null;
        Span<CandidateCircle> selectedCandidates =
            circleCount <= StackCandidateLimit
                ? stackalloc CandidateCircle[circleCount]
                : (rentedCandidates = ArrayPool<CandidateCircle>.Shared.Rent(circleCount)).AsSpan(0, circleCount);

        try
        {
            for (int phaseYIndex = 0; phaseYIndex < PhaseSamples; phaseYIndex++)
            {
                double phaseY = (phaseYIndex / (double)PhaseSamples) * stepY;

                for (int phaseXIndex = 0; phaseXIndex < PhaseSamples; phaseXIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int arrangementIndex =
                        (angleIndex * PhaseSamples * PhaseSamples) + (phaseYIndex * PhaseSamples) + phaseXIndex;
                    double phaseX = (phaseXIndex / (double)PhaseSamples) * stepX;
                    int selectedCount = 0;

                    CollectCandidates(
                        polygon,
                        referencePoint,
                        radius,
                        stepX,
                        stepY,
                        localBounds,
                        phaseX,
                        phaseY,
                        cos,
                        sin,
                        selectedCandidates,
                        ref selectedCount
                    );

                    if (selectedCount < circleCount)
                        continue;

                    ReadOnlySpan<CandidateCircle> selectedSpan = selectedCandidates[..circleCount];
                    double safeRadius = ComputeSafeRadius(selectedSpan, radius);
                    if (safeRadius <= BoundaryEpsilon)
                        continue;

                    double score = ComputeScore(selectedSpan);
                    searchState.TryUpdate(selectedSpan, score, safeRadius, arrangementIndex);
                }
            }
        }
        finally
        {
            if (rentedCandidates is not null)
                ArrayPool<CandidateCircle>.Shared.Return(rentedCandidates, clearArray: false);
        }
    }

    private static void CollectCandidates(
        PolygonData polygon,
        PackingPoint referencePoint,
        double radius,
        double stepX,
        double stepY,
        PackingBounds localBounds,
        double phaseX,
        double phaseY,
        double cos,
        double sin,
        Span<CandidateCircle> selectedCandidates,
        ref int selectedCount
    )
    {
        double startY = localBounds.Top - stepY + phaseY;
        int rowIndex = 0;

        for (double localY = startY; localY <= localBounds.Bottom + stepY; localY += stepY)
        {
            double rowOffset = (rowIndex++ & 1) == 0 ? 0 : stepX / 2.0;
            double startX = localBounds.Left - stepX + phaseX + rowOffset;

            for (double localX = startX; localX <= localBounds.Right + stepX; localX += stepX)
            {
                PackingPoint world = RotateOut(localX, localY, referencePoint, cos, sin);
                double clearance = GetSignedDistanceToBoundary(polygon, world);
                if (clearance + BoundaryEpsilon < radius)
                    continue;

                double dx = world.X - referencePoint.X;
                double dy = world.Y - referencePoint.Y;
                InsertCandidate(
                    selectedCandidates,
                    ref selectedCount,
                    new CandidateCircle(world.X, world.Y, (dx * dx) + (dy * dy), clearance, Math.Atan2(dy, dx))
                );
            }
        }
    }

    private static void InsertCandidate(
        Span<CandidateCircle> selectedCandidates,
        ref int selectedCount,
        CandidateCircle candidate
    )
    {
        if (
            selectedCount == selectedCandidates.Length
            && CompareCandidates(candidate, selectedCandidates[selectedCandidates.Length - 1]) >= 0
        )
        {
            return;
        }

        int insertIndex = selectedCount;
        while (insertIndex > 0 && CompareCandidates(candidate, selectedCandidates[insertIndex - 1]) < 0)
            insertIndex--;

        if (selectedCount < selectedCandidates.Length)
        {
            for (int moveIndex = selectedCount; moveIndex > insertIndex; moveIndex--)
                selectedCandidates[moveIndex] = selectedCandidates[moveIndex - 1];

            selectedCandidates[insertIndex] = candidate;
            selectedCount++;
            return;
        }

        for (int moveIndex = selectedCandidates.Length - 1; moveIndex > insertIndex; moveIndex--)
            selectedCandidates[moveIndex] = selectedCandidates[moveIndex - 1];

        selectedCandidates[insertIndex] = candidate;
    }

    private static int CompareCandidates(CandidateCircle left, CandidateCircle right)
    {
        int distanceComparison = left.DistanceSquared.CompareTo(right.DistanceSquared);
        if (distanceComparison != 0)
            return distanceComparison;

        int clearanceComparison = right.Clearance.CompareTo(left.Clearance);
        if (clearanceComparison != 0)
            return clearanceComparison;

        int angleComparison = left.Angle.CompareTo(right.Angle);
        if (angleComparison != 0)
            return angleComparison;

        int yComparison = left.Y.CompareTo(right.Y);
        return yComparison != 0 ? yComparison : left.X.CompareTo(right.X);
    }

    private static double ComputeScore(ReadOnlySpan<CandidateCircle> candidates)
    {
        double score = 0;

        for (int index = 0; index < candidates.Length; index++)
            score += candidates[index].DistanceSquared - (candidates[index].Clearance * 0.15);

        return score;
    }

    private static double ComputeSafeRadius(ReadOnlySpan<CandidateCircle> candidates, double requestedRadius)
    {
        double safeRadius = requestedRadius;

        for (int index = 0; index < candidates.Length; index++)
            safeRadius = Math.Min(safeRadius, candidates[index].Clearance - BoundaryEpsilon);

        for (int index = 0; index < candidates.Length; index++)
        {
            CandidateCircle left = candidates[index];

            for (int otherIndex = index + 1; otherIndex < candidates.Length; otherIndex++)
            {
                CandidateCircle right = candidates[otherIndex];
                double dx = left.X - right.X;
                double dy = left.Y - right.Y;
                safeRadius = Math.Min(safeRadius, (Math.Sqrt((dx * dx) + (dy * dy)) / 2.0) - BoundaryEpsilon);
            }
        }

        return safeRadius;
    }

    private static bool ShouldDispatchAsync(int circleCount) =>
        circleCount >= ParallelSearchCircleThreshold && Environment.ProcessorCount > 1;

    private static bool ShouldUseParallelSearch(int circleCount) =>
        circleCount >= ParallelSearchCircleThreshold && Environment.ProcessorCount > 1;

    private static PackingPoint EstimatePackingCenter(PolygonData polygon)
    {
        PackingPoint best = polygon.Centroid;
        double bestDistance = GetSignedDistanceToBoundary(polygon, best);
        double searchLeft = polygon.Bounds.Left;
        double searchTop = polygon.Bounds.Top;
        double searchRight = polygon.Bounds.Right;
        double searchBottom = polygon.Bounds.Bottom;

        for (int pass = 0; pass < CenterSearchRefinementPasses; pass++)
        {
            double width = searchRight - searchLeft;
            double height = searchBottom - searchTop;
            double stepX = width / CenterSearchGridSize;
            double stepY = height / CenterSearchGridSize;

            for (int yIndex = 0; yIndex <= CenterSearchGridSize; yIndex++)
            {
                double y = searchTop + (yIndex * stepY);

                for (int xIndex = 0; xIndex <= CenterSearchGridSize; xIndex++)
                {
                    double x = searchLeft + (xIndex * stepX);
                    PackingPoint candidate = new(x, y);
                    double signedDistance = GetSignedDistanceToBoundary(polygon, candidate);
                    double dx = candidate.X - polygon.Centroid.X;
                    double dy = candidate.Y - polygon.Centroid.Y;
                    double centroidDistance = (dx * dx) + (dy * dy);
                    double bestDx = best.X - polygon.Centroid.X;
                    double bestDy = best.Y - polygon.Centroid.Y;
                    double bestCentroidDistance = (bestDx * bestDx) + (bestDy * bestDy);

                    if (
                        signedDistance > bestDistance
                        || (
                            Math.Abs(signedDistance - bestDistance) <= BoundaryEpsilon
                            && centroidDistance < bestCentroidDistance
                        )
                    )
                    {
                        best = candidate;
                        bestDistance = signedDistance;
                    }
                }
            }

            searchLeft = best.X - stepX;
            searchRight = best.X + stepX;
            searchTop = best.Y - stepY;
            searchBottom = best.Y + stepY;
        }

        return best;
    }

    private static PolygonData CreatePolygonData(IReadOnlyList<Point> vertices)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        double twiceArea = 0;
        double centroidX = 0;
        double centroidY = 0;

        for (int index = 0; index < vertices.Count; index++)
        {
            Point current = vertices[index];
            Point next = vertices[(index + 1) % vertices.Count];
            double cross = (current.X * next.Y) - (next.X * current.Y);

            twiceArea += cross;
            centroidX += (current.X + next.X) * cross;
            centroidY += (current.Y + next.Y) * cross;
            minX = Math.Min(minX, current.X);
            minY = Math.Min(minY, current.Y);
            maxX = Math.Max(maxX, current.X);
            maxY = Math.Max(maxY, current.Y);
        }

        double area = Math.Abs(twiceArea) / 2.0;
        PackingPoint centroid =
            Math.Abs(twiceArea) <= BoundaryEpsilon
                ? new PackingPoint((minX + maxX) / 2.0, (minY + maxY) / 2.0)
                : new PackingPoint(centroidX / (3.0 * twiceArea), centroidY / (3.0 * twiceArea));

        return new PolygonData(
            vertices.Select(static point => new PackingPoint(point.X, point.Y)).ToArray(),
            new PackingBounds(minX, minY, maxX, maxY),
            centroid,
            area
        );
    }

    private static PackingBounds GetRotatedBounds(
        IReadOnlyList<PackingPoint> vertices,
        PackingPoint origin,
        double cos,
        double sin
    )
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (PackingPoint vertex in vertices)
        {
            PackingPoint local = RotateInto(vertex, origin, cos, sin);
            minX = Math.Min(minX, local.X);
            minY = Math.Min(minY, local.Y);
            maxX = Math.Max(maxX, local.X);
            maxY = Math.Max(maxY, local.Y);
        }

        return new PackingBounds(minX, minY, maxX, maxY);
    }

    private static PackingPoint RotateInto(PackingPoint point, PackingPoint origin, double cos, double sin)
    {
        double dx = point.X - origin.X;
        double dy = point.Y - origin.Y;
        return new PackingPoint((dx * cos) + (dy * sin), (-dx * sin) + (dy * cos));
    }

    private static PackingPoint RotateOut(double x, double y, PackingPoint origin, double cos, double sin) =>
        new((x * cos) - (y * sin) + origin.X, (x * sin) + (y * cos) + origin.Y);

    private static double GetSignedDistanceToBoundary(PolygonData polygon, PackingPoint point)
    {
        double boundaryDistance = GetBoundaryDistance(polygon.Vertices, point);
        return IsPointInsidePolygon(polygon.Vertices, point) ? boundaryDistance : -boundaryDistance;
    }

    private static double GetBoundaryDistance(IReadOnlyList<PackingPoint> vertices, PackingPoint point)
    {
        double minimumDistanceSquared = double.MaxValue;

        for (int index = 0; index < vertices.Count; index++)
        {
            PackingPoint current = vertices[index];
            PackingPoint next = vertices[(index + 1) % vertices.Count];
            minimumDistanceSquared = Math.Min(
                minimumDistanceSquared,
                DistanceSquaredToSegment(point.X, point.Y, current.X, current.Y, next.X, next.Y)
            );
        }

        return Math.Sqrt(minimumDistanceSquared);
    }

    private static bool IsPointInsidePolygon(IReadOnlyList<PackingPoint> vertices, PackingPoint point)
    {
        bool inside = false;

        for (int index = 0, previousIndex = vertices.Count - 1; index < vertices.Count; previousIndex = index++)
        {
            PackingPoint current = vertices[index];
            PackingPoint previous = vertices[previousIndex];
            bool intersects =
                ((current.Y > point.Y) != (previous.Y > point.Y))
                && (
                    point.X
                    < (((previous.X - current.X) * (point.Y - current.Y)) / (previous.Y - current.Y)) + current.X
                );

            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    private static double DistanceSquaredToSegment(double px, double py, double x0, double y0, double x1, double y1)
    {
        double dx = x1 - x0;
        double dy = y1 - y0;
        if (Math.Abs(dx) <= double.Epsilon && Math.Abs(dy) <= double.Epsilon)
            return ((px - x0) * (px - x0)) + ((py - y0) * (py - y0));

        double t = (((px - x0) * dx) + ((py - y0) * dy)) / ((dx * dx) + (dy * dy));
        t = Math.Clamp(t, 0.0, 1.0);

        double closestX = x0 + (t * dx);
        double closestY = y0 + (t * dy);
        double offsetX = px - closestX;
        double offsetY = py - closestY;
        return (offsetX * offsetX) + (offsetY * offsetY);
    }

    private readonly record struct PolygonData(
        IReadOnlyList<PackingPoint> Vertices,
        PackingBounds Bounds,
        PackingPoint Centroid,
        double Area
    );

    private readonly record struct PackingPoint(double X, double Y);

    private readonly record struct PackingBounds(double Left, double Top, double Right, double Bottom)
    {
        public double Width => Right - Left;

        public double Height => Bottom - Top;
    }

    private readonly record struct CandidateCircle(
        double X,
        double Y,
        double DistanceSquared,
        double Clearance,
        double Angle
    );

    private sealed class SearchWorkerState : IDisposable
    {
        private readonly CandidateCircle[] _bestCandidates;
        private readonly int _circleCount;

        public SearchWorkerState(int circleCount)
        {
            _circleCount = circleCount;
            _bestCandidates = ArrayPool<CandidateCircle>.Shared.Rent(circleCount);
        }

        private int BestArrangementIndex { get; set; } = int.MaxValue;

        private int BestCount { get; set; }

        private double BestRadius { get; set; }

        private double BestScore { get; set; } = double.MaxValue;

        public void Dispose() => ArrayPool<CandidateCircle>.Shared.Return(_bestCandidates, clearArray: false);

        public PackingCandidate BuildCandidate()
        {
            if (BestCount != _circleCount)
                return PackingCandidate.Empty;

            PackedCircle[] circles = new PackedCircle[_circleCount];
            for (int index = 0; index < _circleCount; index++)
            {
                CandidateCircle candidate = _bestCandidates[index];
                circles[index] = new PackedCircle(candidate.X, candidate.Y, BestRadius);
            }

            return new PackingCandidate(circles, BestScore, BestArrangementIndex);
        }

        public void TryUpdate(
            ReadOnlySpan<CandidateCircle> selectedCandidates,
            double score,
            double radius,
            int arrangementIndex
        )
        {
            if (selectedCandidates.Length != _circleCount)
                return;

            int scoreComparison = score.CompareTo(BestScore);
            if (
                BestCount == _circleCount
                && (scoreComparison > 0 || (scoreComparison == 0 && arrangementIndex >= BestArrangementIndex))
            )
                return;

            selectedCandidates.CopyTo(_bestCandidates);
            BestCount = selectedCandidates.Length;
            BestScore = score;
            BestRadius = radius;
            BestArrangementIndex = arrangementIndex;
        }
    }

    private readonly record struct PackingCandidate(
        IReadOnlyList<PackedCircle> Circles,
        double Score,
        int ArrangementIndex
    )
    {
        public static PackingCandidate Empty { get; } = new([], double.MaxValue, int.MaxValue);

        public bool IsBetterThan(PackingCandidate other)
        {
            if (other.Circles.Count == 0)
                return false;

            if (Circles.Count == 0)
                return true;

            if (other.Circles.Count != Circles.Count)
                return other.Circles.Count > Circles.Count;

            int scoreComparison = other.Score.CompareTo(Score);
            return scoreComparison < 0 || (scoreComparison == 0 && other.ArrangementIndex < ArrangementIndex);
        }
    }
}

[assembly: Timeout(60_000)]

namespace OutbreakTracker2.UnitTests;

public sealed class GeometryPackingParallelLimit : TUnit.Core.Interfaces.IParallelLimit
{
    public int Limit => 1;
}

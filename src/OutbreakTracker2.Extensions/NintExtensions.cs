using System.Runtime.CompilerServices;

namespace OutbreakTracker2.Extensions;

public static class NintExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNull(this nint nativeInteger)
        => nativeInteger != nint.Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNegative(this nint nativeInteger)
        => nativeInteger >= nint.Zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNeitherNullNorNegative(this nint nativeInteger)
        => IsNotNull(nativeInteger) && IsNotNegative(nativeInteger);
}
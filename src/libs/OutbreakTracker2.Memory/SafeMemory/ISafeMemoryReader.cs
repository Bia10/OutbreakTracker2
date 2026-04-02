using System.Diagnostics.CodeAnalysis;

namespace OutbreakTracker2.Memory.SafeMemory;

public interface ISafeMemoryReader
{
    public T Read<T>(nint hProcess, nint address)
        where T : unmanaged;

    public T ReadStruct<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors
        )]
            T
    >(nint hProcess, nint address)
        where T : struct;
}

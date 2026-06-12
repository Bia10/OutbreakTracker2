namespace OutbreakTracker2.PCSX2.EEmem;

public static class EEmemOffsetResolver
{
    public static nint Resolve(nint baseAddress, nint ptrOffset, params ReadOnlySpan<nint> offsets)
    {
        nint resolved = baseAddress + ptrOffset;
        foreach (nint offset in offsets)
        {
            resolved += offset;
        }

        return resolved;
    }
}

namespace OutbreakTracker2.Outbreak.Extensions;

public static class NintExtensions
{
    // This is just basic validation against negative values and NULL pointers.
    // TODO: An actual validation for address would require to precisly locate the address as a member of EEmem memory region
    public static bool IsValidAddress(this nint address)
        => address > nint.Zero;
}
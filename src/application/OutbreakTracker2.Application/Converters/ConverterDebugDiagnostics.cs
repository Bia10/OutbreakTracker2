using System.Diagnostics;

namespace OutbreakTracker2.Application.Converters;

internal static class ConverterDebugDiagnostics
{
    [Conditional("DEBUG")]
    public static void ReportUnexpectedValueType(string converterName, string expectedTypeDescription, object? value)
    {
        if (value is null)
            return;

        Debug.WriteLine($"{converterName} expected {expectedTypeDescription} but received {value.GetType().FullName}.");
    }
}

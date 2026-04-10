using Avalonia.Data.Converters;

namespace OutbreakTracker2.Application.Converters;

public static class BoolToOpacityConverter
{
    /// <summary>
    /// Returns 1.0 when the bound bool is true, 0.5 when false.
    /// Mirrors the original OutbreakTracker behaviour: players that are enabled but not
    /// currently in the active room are shown at half opacity instead of being hidden.
    /// </summary>
    public static readonly FuncValueConverter<bool, double> InGame = new(isInGame => isInGame ? 1.0 : 0.5);
}

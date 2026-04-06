using System.Globalization;
using Avalonia.Data.Converters;

namespace OutbreakTracker2.Application.Views.GameDock.Converters;

/// <summary>
/// Takes a list of values and returns the first non-null value.
/// Used by PinnedDockControl to resolve the window background.
/// </summary>
public sealed class EitherNotNullConverter : IMultiValueConverter
{
    public static readonly EitherNotNullConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        foreach (var value in values)
        {
            if (value is not null)
                return value;
        }

        return values;
    }
}

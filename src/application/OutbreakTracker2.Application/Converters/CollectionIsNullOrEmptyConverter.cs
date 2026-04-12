using System.Collections;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace OutbreakTracker2.Application.Converters;

public sealed class CollectionIsNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Treat null explicitly as empty — a null collection is null-or-empty.
        if (value is null)
            return parameter as string is "Inverse" ? false : (object)true;

        if (value is not ICollection collection)
        {
            ConverterDebugDiagnostics.ReportUnexpectedValueType(
                nameof(CollectionIsNullOrEmptyConverter),
                "ICollection",
                value
            );
            return BindingOperations.DoNothing;
        }

        if (parameter as string is "Inverse")
            return collection.Count > 0;

        return collection.Count is 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
}

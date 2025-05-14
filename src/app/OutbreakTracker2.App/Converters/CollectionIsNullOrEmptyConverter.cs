using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Globalization;

namespace OutbreakTracker2.App.Converters;

public sealed class CollectionIsNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ICollection collection)
            return BindingOperations.DoNothing;

        if (parameter as string is "Inverse")
            return collection.Count > 0;

        return collection.Count is 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
}
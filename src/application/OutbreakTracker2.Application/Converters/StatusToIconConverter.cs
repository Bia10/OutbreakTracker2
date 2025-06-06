using Avalonia.Data;
using Avalonia.Data.Converters;
using Material.Icons;
using System;
using System.Globalization;

namespace OutbreakTracker2.Application.Converters;

public sealed class StatusToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string rawStatus)
            return BindingOperations.DoNothing;

        return rawStatus switch
        {
            "OK" => MaterialIconKind.Success,
            "Dead" => MaterialIconKind.Error,
            "Zombie" => MaterialIconKind.Error,
            "Down" => MaterialIconKind.Warning,
            "Gas" => MaterialIconKind.Warning,
            "Bleed" => MaterialIconKind.Warning,
            "" => MaterialIconKind.Information,
            _ => MaterialIconKind.Error
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
}

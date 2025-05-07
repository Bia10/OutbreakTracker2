using Avalonia.Data;
using Avalonia.Data.Converters;
using Material.Icons;
using System;
using System.Globalization;

namespace OutbreakTracker2.App.Converters;

public class ConditionToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string rawCondition)    
            return BindingOperations.DoNothing;    

        return rawCondition.ToLower() switch
        {
            "fine" => MaterialIconKind.Success,
            "caution2" => MaterialIconKind.Warning,
            "caution" => MaterialIconKind.Warning,
            "gas" => MaterialIconKind.Warning,
            "danger" => MaterialIconKind.Error,
            "down" => MaterialIconKind.Error,
            "down+gas" => MaterialIconKind.Error,
            "" => MaterialIconKind.Information,
            _ => MaterialIconKind.Error
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
}

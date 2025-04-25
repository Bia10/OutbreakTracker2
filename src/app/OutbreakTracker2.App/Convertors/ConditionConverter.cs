using System;
using System.Globalization;
using Avalonia.Controls.Notifications;
using Avalonia.Data.Converters;

namespace OutbreakTracker2.App.Convertors;

public sealed class ConditionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as string) switch
        {
            "danger" => NotificationType.Error,
            "caution2" => NotificationType.Warning,
            "caution" => NotificationType.Information,
            _ => NotificationType.Success
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
using System;
using System.Globalization;
using Avalonia.Controls.Notifications;
using Avalonia.Data.Converters;

namespace OutbreakTracker2.App.Convertors;

public sealed class StatusToConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as string) switch
        {
            "Dead" => NotificationType.Error,
            "Zombie" => NotificationType.Warning,
            "Down" => NotificationType.Warning,
            "Gas" => NotificationType.Warning,
            "Bleed" => NotificationType.Warning,
            _ => NotificationType.Information
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
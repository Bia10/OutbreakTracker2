using Avalonia.Data;
using Avalonia.Data.Converters;
using Material.Icons;
using System;
using System.Globalization;

namespace OutbreakTracker2.Application.Converters;

public sealed class LobbyVersionToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string versionString || string.IsNullOrWhiteSpace(versionString))
            return BindingOperations.DoNothing;

        return versionString.ToLower() switch
        {
            "dvd-rom" => MaterialIconKind.Album,
            "hdd" => MaterialIconKind.Harddisk,
            _ => MaterialIconKind.HelpCircleOutline
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new BindingNotification(new NotSupportedException(), BindingErrorType.Error);
}

using Avalonia.Data;
using Avalonia.Data.Converters;
using Material.Icons;
using System;
using System.Globalization;

namespace OutbreakTracker2.App.Converters;

public sealed class LobbyVersionToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string versionString || string.IsNullOrWhiteSpace(versionString)
            || versionString.Equals("Unknown", StringComparison.Ordinal))
            return BindingOperations.DoNothing;

        string lowerVersion = versionString.Trim().ToLowerInvariant();
        return lowerVersion switch
        {
            "dvd-rom" => MaterialIconKind.Album,
            "hdd" => MaterialIconKind.Harddisk,
            _ => MaterialIconKind.HelpCircleOutline
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new BindingNotification(new NotSupportedException(), BindingErrorType.Error);
}

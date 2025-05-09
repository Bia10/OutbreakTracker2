using Avalonia.Data;
using Avalonia.Data.Converters;
using Material.Icons;
using System;
using System.Globalization;

namespace OutbreakTracker2.App.Converters;

public static class BoolToIconConverters
{
    public static readonly BoolToIconConverter Animation = new(MaterialIconKind.Pause, MaterialIconKind.Play);
    public static readonly BoolToIconConverter WindowLock = new(MaterialIconKind.Unlocked, MaterialIconKind.Lock);
    public static readonly BoolToIconConverter Visibility = new(MaterialIconKind.EyeClosed, MaterialIconKind.Eye);
    public static readonly BoolToIconConverter Simple = new(MaterialIconKind.Close, MaterialIconKind.Ticket);
    public static readonly BoolToIconConverter Password = new(MaterialIconKind.Lock, MaterialIconKind.Unlocked);
}

public sealed class BoolToIconConverter(MaterialIconKind trueIcon, MaterialIconKind falseIcon) : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool b)
            return BindingOperations.DoNothing;

        return b ? trueIcon : falseIcon;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
}
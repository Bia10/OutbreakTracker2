using Avalonia.Data;
using Avalonia.Data.Converters;
using Material.Icons;
using System;
using System.Globalization;

namespace OutbreakTracker2.Application.Converters
{
    public sealed class BoolToIconConverter(MaterialIconKind trueIcon, MaterialIconKind falseIcon) : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool b)
                return BindingOperations.DoNothing;

            return b ? trueIcon : falseIcon;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
}
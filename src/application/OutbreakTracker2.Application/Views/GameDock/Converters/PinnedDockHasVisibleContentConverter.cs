using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Dock.Model.Controls;

namespace OutbreakTracker2.Application.Views.GameDock.Converters;

public sealed class PinnedDockHasVisibleContentConverter : IValueConverter
{
    public static readonly PinnedDockHasVisibleContentConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IToolDock dock && !dock.IsEmpty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new BindingNotification(new InvalidOperationException(), BindingErrorType.Error);
}

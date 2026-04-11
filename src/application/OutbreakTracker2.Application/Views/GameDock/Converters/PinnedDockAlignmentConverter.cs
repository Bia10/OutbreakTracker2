using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Dock.Model.Controls;
using Dock.Model.Core;

namespace OutbreakTracker2.Application.Views.GameDock.Converters;

public sealed class PinnedDockAlignmentConverter : IValueConverter
{
    public static readonly PinnedDockAlignmentConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IToolDock dock ? dock.Alignment : Alignment.Left;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new BindingNotification(new InvalidOperationException(), BindingErrorType.Error);
}

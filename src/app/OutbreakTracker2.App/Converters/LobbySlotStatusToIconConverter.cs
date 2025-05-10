using Avalonia.Data;
using Avalonia.Data.Converters;
using FastEnumUtility;
using Material.Icons;
using OutbreakTracker2.Outbreak.Enums.LobbySlot;
using System;
using System.Globalization;

namespace OutbreakTracker2.App.Converters;

public sealed class LobbySlotStatusToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return BindingOperations.DoNothing;

        SlotStatus status;

        switch (value)
        {
            case SlotStatus directStatus:
                status = directStatus;
                break;
            case string statusString when FastEnum.TryParse(statusString, true, out SlotStatus parsedStatus):
                status = parsedStatus;
                break;
            case string statusString when statusString.Equals("Join in", StringComparison.OrdinalIgnoreCase):
                status = SlotStatus.JoinIn;
                break;
            case string: status = SlotStatus.Unknown;
                break;

            default: return BindingOperations.DoNothing;
        }

        return status switch
        {
            SlotStatus.Unknown => MaterialIconKind.HelpCircleOutline,
            SlotStatus.Vacant => MaterialIconKind.CheckboxBlankCircleOutline,
            SlotStatus.Busy => MaterialIconKind.ProgressClock,
            SlotStatus.JoinIn => MaterialIconKind.DoorOpen,
            SlotStatus.Full => MaterialIconKind.AccountMultipleRemoveOutline,
            _ => MaterialIconKind.HelpCircleOutline
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new BindingNotification(new NotSupportedException(), BindingErrorType.Error);
}
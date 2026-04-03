using Avalonia;
using Avalonia.Controls;

namespace OutbreakTracker2.Application.Views.Common;

public partial class NotInGameWatermark : UserControl
{
    public static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<
        NotInGameWatermark,
        string
    >(nameof(Message), defaultValue: "Not in-game");

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public NotInGameWatermark()
    {
        InitializeComponent();
    }
}

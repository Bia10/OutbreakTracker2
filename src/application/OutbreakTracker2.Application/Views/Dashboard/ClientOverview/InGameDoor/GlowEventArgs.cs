using Avalonia.Media;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameDoor;

public sealed class GlowEventArgs(Color color) : EventArgs
{
    public Color Color { get; } = color;
}

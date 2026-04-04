using Avalonia.Media;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.Inventory;

public sealed class GlowEventArgs(Color color) : EventArgs
{
    public Color Color { get; } = color;
}

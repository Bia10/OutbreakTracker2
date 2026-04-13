using Avalonia.Media;

namespace OutbreakTracker2.Application.Views.Common;

public sealed class GlowEventArgs(Color color) : EventArgs
{
    public Color Color { get; } = color;
}

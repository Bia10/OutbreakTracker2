namespace OutbreakTracker2.Application.Services.PlayerTracking;

public sealed class PlayerStateChangeEventArgs(string message, string title, ToastType type) : EventArgs
{
    public string Message { get; } = message;
    public string Title { get; } = title;
    public ToastType Type { get; } = type;
}

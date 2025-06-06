using System;

namespace OutbreakTracker2.Application.Services.PlayerTracking;

public sealed class PlayerStateChangeEventArgs : EventArgs
{
    public string Message { get; }
    public string Title { get; }
    public ToastType Type { get; }

    public PlayerStateChangeEventArgs(string message, string title, ToastType type)
    {
        Message = message;
        Title = title;
        Type = type;
    }
}

public enum ToastType
{
    Info,
    Warning,
    Error,
    Success
}
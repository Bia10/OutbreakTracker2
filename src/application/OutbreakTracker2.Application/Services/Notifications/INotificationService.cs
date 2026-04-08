namespace OutbreakTracker2.Application.Services.Notifications;

/// <summary>
/// Coordinates cross-cutting notification side-effects (player state toasts, etc.).
/// Implemented as a singleton that subscribes to data streams on construction.
/// </summary>
public interface INotificationService : IDisposable;

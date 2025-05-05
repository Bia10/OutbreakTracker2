using Microsoft.Extensions.Logging;
using System;

namespace OutbreakTracker2.App.Views.Logging;

public sealed record LogModel
{
    public DateTime Timestamp { get; init; }
    public LogLevel LogLevel { get; init; }
    public EventId EventId { get; init; }
    public object? State { get; init; }
    public string? Exception { get; init; }
    public string DisplayMessage { get; }
    public string DisplayException { get; }

    private LogModel(
        DateTime timestamp,
        LogLevel logLevel,
        EventId eventId,
        object? state,
        string? exception,
        string displayMessage,
        string displayException)
    {
        Timestamp = timestamp;
        LogLevel = logLevel;
        EventId = eventId;
        State = state;
        Exception = exception;
        DisplayMessage = displayMessage;
        DisplayException = displayException;
    }

    public static LogModel ToDisplayForm(LogModel originalLog)
    {
        string displayMessage = originalLog.State?.ToString() ?? "(null)";
        string displayException = !string.IsNullOrEmpty(originalLog.Exception)
            ? FormatExceptionForDisplay(originalLog.Exception)
            : string.Empty;

        return new LogModel(
            originalLog.Timestamp,
            originalLog.LogLevel,
            originalLog.EventId,
            originalLog.State,
            originalLog.Exception,
            displayMessage,
            displayException
        );
    }

    private static string FormatExceptionForDisplay(string exception)
        => exception;

    public LogModel(
        DateTime timestamp,
        LogLevel logLevel,
        EventId eventId,
        object? state,
        string? exception)
    {
        Timestamp = timestamp;
        LogLevel = logLevel;
        EventId = eventId;
        State = state;
        Exception = exception;
        DisplayMessage = string.Empty;
        DisplayException = string.Empty;
    }

    public LogModel(
        LogLevel logLevel,
        EventId eventId,
        object? state,
        string? exception)
    {
        Timestamp = DateTime.UtcNow;
        LogLevel = logLevel;
        EventId = eventId;
        State = state;
        Exception = exception;
        DisplayMessage = string.Empty;
        DisplayException = string.Empty;
    }
}

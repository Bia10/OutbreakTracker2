using Microsoft.Extensions.Logging;
using System;

namespace OutbreakTracker2.App.Views.Logging;

public record LogModel
{
    public DateTime Timestamp { get; set; }

    public LogLevel LogLevel { get; set; }

    public EventId EventId { get; set; }

    public object? State { get; set; }

    public string? Exception { get; set; }
}

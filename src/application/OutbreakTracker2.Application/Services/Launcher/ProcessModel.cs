using System;

namespace OutbreakTracker2.Application.Services.Launcher;

public sealed record ProcessModel
{
    public string? Name { get; init; }

    public int Id { get; init; }

    public bool IsRunning { get; init; }

    public DateTime StartTime { get; init; }

    public int ExitCode { get; set; }
}

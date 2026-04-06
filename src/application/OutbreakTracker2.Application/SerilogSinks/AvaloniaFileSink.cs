using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Logging;

namespace OutbreakTracker2.Application.SerilogSinks;

/// <summary>
/// Avalonia <see cref="ILogSink"/> that writes directly to a dedicated log file.
/// Captures binding errors, property coercion failures, control theme resolution
/// issues, and other Avalonia-internal diagnostics that do not flow through
/// Serilog / MEL because Avalonia uses its own logging pipeline.
/// </summary>
internal sealed partial class AvaloniaFileSink : ILogSink, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly Lock _lock = new();

    public AvaloniaFileSink(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _writer = new StreamWriter(filePath, append: true, Encoding.UTF8) { AutoFlush = true };
    }

    public bool IsEnabled(LogEventLevel level, string area)
    {
        // Warning+ for everything; Verbose+ only for Binding area (most useful for dock debugging).
        return level >= LogEventLevel.Warning || area is "Binding";
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Write(level, area, source, messageTemplate);
    }

    public void Log(
        LogEventLevel level,
        string area,
        object? source,
        string messageTemplate,
        params object?[] propertyValues
    )
    {
        // Avalonia uses Serilog-style named placeholders like {Property}, {$Value}.
        // Replace them sequentially with the positional values provided.
        string message = FormatNamedTemplate(messageTemplate, propertyValues);
        Write(level, area, source, message);
    }

    private static string FormatNamedTemplate(string template, object?[] values)
    {
        if (values.Length == 0)
            return template;

        int index = 0;
        return NamedPlaceholderRegex()
            .Replace(template, match => index < values.Length ? values[index++]?.ToString() ?? "null" : match.Value);
    }

    [GeneratedRegex(@"\{[$]?\w+\}", RegexOptions.NonBacktracking)]
    private static partial Regex NamedPlaceholderRegex();

    private void Write(LogEventLevel level, string area, object? source, string message)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        string sourceName = source?.GetType().Name ?? "null";
        string line = $"[{timestamp}] [{level}] [{area}] [{sourceName}] {message}";

        lock (_lock)
        {
            _writer.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
        }
    }
}

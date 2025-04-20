using Microsoft.Extensions.Logging;
using OutbreakTracker2.App.Views.Logging;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using OutbreakTracker2.App.Services.LogStorage;

namespace OutbreakTracker2.App.SerilogSinks;

public class DataStoreLoggerSink : ILogEventSink, IDisposable
{
    private readonly ILogDataStorageService _dataStore;
    private readonly DataStoreLoggerConfiguration _config;
    private readonly IFormatProvider? _formatProvider;
    private readonly ILogger<DataStoreLoggerSink>? _logger;
    private readonly Channel<LogEvent> _logEventChannel;
    private readonly Task _backgroundTask;
    private readonly CancellationTokenSource _cts = new();

    public DataStoreLoggerSink(
        ILogDataStorageService dataStore,
        DataStoreLoggerConfiguration? config = null,
        IFormatProvider? formatProvider = null,
        ILogger<DataStoreLoggerSink>? logger = null)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _config = config ?? new DataStoreLoggerConfiguration();
        _formatProvider = formatProvider;
        _logger = logger;

        _logEventChannel = Channel.CreateUnbounded<LogEvent>(new UnboundedChannelOptions
        {
            SingleWriter = true, // Only the sink will write to the channel
            SingleReader = false // Allow multiple readers (though we'll only use one)
        });

        _backgroundTask = Task.Run(() => ProcessLogEventsAsync(_cts.Token));
    }

    public void Emit(LogEvent logEvent)
    {
        if (!_logEventChannel.Writer.TryWrite(logEvent))
            _logger?.LogWarning("Failed to enqueue log event. Channel may be closed");
    }

    private async Task ProcessLogEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _logEventChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            while (_logEventChannel.Reader.TryRead(out LogEvent? logEvent))
                try
                {
                    await ProcessLogEventAsync(logEvent, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogInformation("Log event processing was canceled");
                    throw;
                }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Log event processing was canceled");
        }
    }

    private Task ProcessLogEventAsync(LogEvent logEvent, CancellationToken cancellationToken)
    {
        LogLevel logLevel = MapLogLevel(logEvent.Level);
        EventId eventId = GetEventId(logEvent);
        (string message, string exception) = GetMessageAndException(logEvent);

        return AddLogEntryAsync(logLevel, eventId, message, exception, cancellationToken);
    }

    private static LogLevel MapLogLevel(LogEventLevel logEventLevel)
    {
        return logEventLevel switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }

    private EventId GetEventId(LogEvent logEvent)
    {
        EventId eventId = EventIdFactory(logEvent);
        if (eventId.Id is 0 && _config.EventId != 0)
            eventId = _config.EventId;

        return eventId;
    }

    private (string Message, string Exception) GetMessageAndException(LogEvent logEvent)
    {
        string message = logEvent.RenderMessage(_formatProvider);
        string exception = logEvent.Exception?.Message ?? (logEvent.Level >= LogEventLevel.Error ? message : string.Empty);

        return (message, exception);
    }

    private async Task AddLogEntryAsync(LogLevel logLevel, EventId eventId, string message, string exception, CancellationToken cancellationToken)
    {
        try
        {
            await _dataStore.AddEntryAsync(new LogModel
            {
                Timestamp = DateTime.UtcNow,
                LogLevel = logLevel,
                EventId = eventId,
                State = message,
                Exception = exception,
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Log entry addition was canceled");
            throw;
        }
    }

    private static EventId EventIdFactory(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("EventId", out LogEventPropertyValue? src) || src is not StructureValue value)
            return new EventId();

        int? id = null;
        string? eventName = null;

        LogEventProperty? idProperty = value.Properties.FirstOrDefault(x => x.Name.Equals("Id"));
        if (idProperty is not null)
            id = int.Parse(idProperty.Value.ToString());

        LogEventProperty? nameProperty = value.Properties.FirstOrDefault(x => x.Name.Equals("Name"));
        if (nameProperty is not null)
            eventName = nameProperty.Value.ToString().Trim('"');

        return new EventId(id ?? 0, eventName ?? string.Empty);
    }

    public async void Dispose()
    {
        try
        {
            await _cts.CancelAsync().ConfigureAwait(false);

            _logEventChannel.Writer.Complete();

            try
            {
                await _backgroundTask.ConfigureAwait(false);
            }
            catch (AggregateException ex)
            {
                _logger?.LogError(ex.InnerException ?? ex, "An error occurred while awaiting end of background task");
            }
            finally
            {
                _cts.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.InnerException ?? ex, "An error occurred while disposing the sink");
        }

        GC.SuppressFinalize(this);
    }
}

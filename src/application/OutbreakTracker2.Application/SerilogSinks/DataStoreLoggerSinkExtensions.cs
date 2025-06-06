using OutbreakTracker2.Application.Services.LogStorage;
using Serilog;
using Serilog.Configuration;
using System;

namespace OutbreakTracker2.Application.SerilogSinks;

public static class DataStoreLoggerSinkExtensions
{
    public static LoggerConfiguration DataStoreLoggerSink(
        this LoggerSinkConfiguration loggerConfiguration,
        ILogDataStorageService dataStore,
        Action<DataStoreLoggerConfiguration>? configuration = null,
        IFormatProvider? formatProvider = null,
        ILogger? logger = null)
    {
        DataStoreLoggerConfiguration config = new();

        try
        {
            configuration?.Invoke(config);
        }
        catch (Exception ex)
        {
            logger?.Error(ex, "Exception occurred while configuring DataStoreLoggerSink");
            throw;
        }

        return loggerConfiguration.Sink(new DataStoreLoggerSink(dataStore, config, formatProvider));
    }
}

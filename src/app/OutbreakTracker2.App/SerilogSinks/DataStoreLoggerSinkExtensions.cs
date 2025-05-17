using Serilog.Configuration;
using Serilog;
using System;
using OutbreakTracker2.App.Services.LogStorage;

namespace OutbreakTracker2.App.SerilogSinks;

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
            logger?.Error("Exception occurred while configuring DataStoreLoggerSink ex: {Ex}", ex);
            throw;
        }

        return loggerConfiguration.Sink(new DataStoreLoggerSink(dataStore, config, formatProvider));
    }
}

using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.App.SerilogSinks;

public readonly record struct DataStoreLoggerConfiguration(EventId EventId);

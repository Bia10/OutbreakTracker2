using Microsoft.Extensions.Logging;

namespace OutbreakTracker2.Application.SerilogSinks;

public readonly record struct DataStoreLoggerConfiguration(EventId EventId);

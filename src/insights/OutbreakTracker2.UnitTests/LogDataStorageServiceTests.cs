using System.Reflection;
using System.Threading.Channels;
using Bia.LogViewer.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Services.LogStorage;

namespace OutbreakTracker2.UnitTests;

public sealed class LogDataStorageServiceTests
{
    [Test]
    public async Task AddEntryAsync_IgnoresClosedChannel_DuringShutdownRace()
    {
        await using LogDataStorageService service = new(
            NullLogger<LogDataStorageService>.Instance,
            new ImmediateDispatcherService()
        );

        Channel<LogModel> logChannel = GetLogChannel(service);
        logChannel.Writer.TryComplete();

        LogModel logEntry = new()
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = LogLevel.Information,
            EventId = new EventId(7, "Shutdown"),
            Message = "Ignore closed channel race",
        };

        await service.AddEntryAsync(logEntry, CancellationToken.None);

        await Assert.That(service.Entries.Count).IsEqualTo(0);
    }

    private static Channel<LogModel> GetLogChannel(LogDataStorageService service)
    {
        FieldInfo? field = typeof(LogDataStorageService).GetField(
            "_logChannel",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        return field?.GetValue(service) as Channel<LogModel>
            ?? throw new InvalidOperationException("Unable to access the log channel for testing.");
    }

    private sealed class ImmediateDispatcherService : IDispatcherService
    {
        public bool CheckAccess() => true;

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public bool IsOnUIThread()
        {
            throw new NotImplementedException();
        }

        public void PostOnUI(Action action) => action();
    }
}

namespace OutbreakTracker2.MemoryWatcherIntegration;

public interface IMemoryActivitySource
{
    ValueTask<OutbreakTrackerMemoryActivityResult> WaitForActivityAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken
    );

    void Detach();
}

using OutbreakTracker2.Application.Services.Reports.Events;

namespace OutbreakTracker2.Application.Services.Reports;

internal sealed class RunReportProcessingContext
{
    private readonly Action<RunEvent> _emit;
    private readonly Action _autoStartSession;
    private readonly Action _autoStopSession;
    private readonly Func<bool> _isSessionRunning;
    private readonly Func<
        byte,
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)>
    > _findContributingPlayers;
    private readonly Func<short, string> _resolvePickupHolderName;

    public RunReportProcessingContext(
        RunReportProcessingState state,
        TimeProvider timeProvider,
        Action<RunEvent> emit,
        Action autoStartSession,
        Action autoStopSession,
        Func<bool> isSessionRunning,
        Func<byte, IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)>> findContributingPlayers,
        Func<short, string> resolvePickupHolderName
    )
    {
        State = state;
        TimeProvider = timeProvider;
        _emit = emit;
        _autoStartSession = autoStartSession;
        _autoStopSession = autoStopSession;
        _isSessionRunning = isSessionRunning;
        _findContributingPlayers = findContributingPlayers;
        _resolvePickupHolderName = resolvePickupHolderName;
    }

    public RunReportProcessingState State { get; }

    public TimeProvider TimeProvider { get; }

    public DateTimeOffset GetCurrentTime() => TimeProvider.GetUtcNow();

    public void Emit(RunEvent evt) => _emit(evt);

    public void AutoStartSession() => _autoStartSession();

    public void AutoStopSession() => _autoStopSession();

    public bool IsSessionRunning() => _isSessionRunning();

    public IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> FindContributingPlayers(byte enemyRoomId) =>
        _findContributingPlayers(enemyRoomId);

    public string ResolvePickupHolderName(short pickedUp) => _resolvePickupHolderName(pickedUp);
}

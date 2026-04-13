using System.Collections.Concurrent;
using System.Diagnostics;
using Cysharp.Diagnostics;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Application.Utilities;
using OutbreakTracker2.PCSX2.Client;
using R3;

namespace OutbreakTracker2.Application.Services.Launcher;

public sealed class ProcessLauncher(ILogger<ProcessLauncher> logger, IGameClientFactory gameClientFactory)
    : IProcessLauncher,
        IDisposable
{
    private sealed class ProcessOutputRegistration(Task task, CancellationTokenSource cancellationTokenSource)
    {
        public Task Task { get; } = task;

        public CancellationTokenSource CancellationTokenSource { get; } = cancellationTokenSource;
    }

    private static readonly TimeSpan ProcessOutputShutdownTimeout = TimeSpan.FromSeconds(2);
    private readonly ILogger<ProcessLauncher> _logger = logger;
    private readonly IGameClientFactory _gameClientFactory = gameClientFactory;
    private readonly Subject<string> _processErrors = new();
    private readonly Subject<ProcessModel> _processUpdate = new();
    private readonly Subject<bool> _isCancelling = new();
    private readonly Lock _clientStateLock = new();
    private readonly ConcurrentDictionary<int, Process> _processes = new();
    private readonly ConcurrentDictionary<int, byte> _clientProcessIds = new();
    private readonly ConcurrentDictionary<int, ProcessOutputRegistration> _processOutputRegistrations = new();
    private Process? _clientMonitoredProcess;
    private IGameClient? _attachedGameClient;

    public Observable<bool> IsCancelling => _isCancelling.AsObservable();
    public Observable<ProcessModel> ProcessUpdate => _processUpdate.AsObservable();
    public Process? ClientMonitoredProcess
    {
        get
        {
            lock (_clientStateLock)
                return _clientMonitoredProcess;
        }
    }

    public IGameClient? AttachedGameClient
    {
        get
        {
            lock (_clientStateLock)
                return _attachedGameClient;
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName, string? arguments) =>
        new()
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(fileName)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

    private (Process? MonitoredProcess, IGameClient? AttachedGameClient) GetClientStateSnapshot()
    {
        lock (_clientStateLock)
            return (_clientMonitoredProcess, _attachedGameClient);
    }

    private void SetClientMonitoredProcess(Process? process)
    {
        lock (_clientStateLock)
            _clientMonitoredProcess = process;
    }

    private IGameClient? ReplaceAttachedGameClient(IGameClient newClient)
    {
        lock (_clientStateLock)
        {
            IGameClient? previousClient = _attachedGameClient;
            _attachedGameClient = newClient;
            return previousClient;
        }
    }

    private IGameClient? ClearClientStateIfMonitoredProcessMatches(Process process)
    {
        lock (_clientStateLock)
        {
            if (!ReferenceEquals(_clientMonitoredProcess, process))
                return null;

            _clientMonitoredProcess = null;

            IGameClient? attachedGameClient = _attachedGameClient;
            _attachedGameClient = null;
            return attachedGameClient;
        }
    }

    private IGameClient? ClearClientState()
    {
        lock (_clientStateLock)
        {
            IGameClient? attachedGameClient = _attachedGameClient;
            _attachedGameClient = null;
            _clientMonitoredProcess = null;
            return attachedGameClient;
        }
    }

    private void RegisterProcessOutputReader(
        Process process,
        ProcessAsyncEnumerable stdOut,
        ProcessAsyncEnumerable stdError,
        CancellationToken cancellationToken
    )
    {
        CancellationTokenSource outputCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task outputTask = HandleProcessOutputAsync(process, stdOut, stdError, outputCts);

        if (!_processOutputRegistrations.TryAdd(process.Id, new ProcessOutputRegistration(outputTask, outputCts)))
        {
            outputCts.Cancel();
            outputCts.Dispose();
            _logger.LogWarning(
                "Process output reader registration already exists for PID {ProcessId}; skipping duplicate registration",
                process.Id
            );
        }
    }

    private void RemoveProcessOutputRegistration(int processId, CancellationTokenSource cancellationTokenSource)
    {
        if (
            _processOutputRegistrations.TryGetValue(processId, out ProcessOutputRegistration? registration)
            && ReferenceEquals(registration.CancellationTokenSource, cancellationTokenSource)
            && _processOutputRegistrations.TryRemove(processId, out _)
        )
        {
            cancellationTokenSource.Dispose();
        }
    }

    private static bool AllInnerExceptionsAreCancellation(AggregateException aggregateException)
    {
        if (aggregateException.InnerExceptions.Count == 0)
            return false;

        foreach (Exception innerException in aggregateException.InnerExceptions)
        {
            if (innerException is not OperationCanceledException)
                return false;
        }

        return true;
    }

    private void StopProcessOutputReaders()
    {
        KeyValuePair<int, ProcessOutputRegistration>[] registrations = [.. _processOutputRegistrations];
        if (registrations.Length == 0)
            return;

        foreach (KeyValuePair<int, ProcessOutputRegistration> registration in registrations)
            registration.Value.CancellationTokenSource.Cancel();

        Task[] shutdownTasks = new Task[registrations.Length];
        for (int index = 0; index < registrations.Length; index++)
            shutdownTasks[index] = registrations[index].Value.Task;

        try
        {
            if (!Task.WhenAll(shutdownTasks).Wait(ProcessOutputShutdownTimeout))
            {
                _logger.LogWarning(
                    "Timed out waiting for {Count} process output reader(s) to stop during launcher disposal",
                    shutdownTasks.Length
                );
            }
        }
        catch (AggregateException ex) when (AllInnerExceptionsAreCancellation(ex)) { }
        catch (AggregateException ex)
        {
            _logger.LogWarning(ex, "Process output reader shutdown completed with unexpected exceptions.");
        }
        finally
        {
            foreach (KeyValuePair<int, ProcessOutputRegistration> registration in registrations)
            {
                if (_processOutputRegistrations.TryRemove(registration.Key, out _))
                    registration.Value.CancellationTokenSource.Dispose();
            }
        }
    }

    private void PublishCancellationState(bool isCancelling) => _isCancelling.OnNext(isCancelling);

    private void RegisterProcess(Process process)
    {
        process.EnableRaisingEvents = true;
        process.Exited += (sender, _) => HandleProcessExit(sender);

        // TODO: manage multiple instances - for now, only single client monitored
        _processes.TryAdd(process.Id, process);
        _clientProcessIds.TryAdd(process.Id, 0);

        SetClientMonitoredProcess(process);

        _processUpdate.OnNext(
            new ProcessModel
            {
                IsRunning = true,
                Id = process.Id,
                Name = process.ProcessName,
                StartTime = process.GetSafeStartTime(),
            }
        );
    }

    public async Task<IGameClient> LaunchAsync(
        string fileName,
        string? arguments,
        CancellationToken cancellationToken = default
    )
    {
        ProcessStartInfo processStartInfo = CreateProcessStartInfo(fileName, arguments);

        (Process process, ProcessAsyncEnumerable stdOut, ProcessAsyncEnumerable stdError) =
            ProcessX.GetDualAsyncEnumerable(processStartInfo);

        IGameClient attachedGameClient = await ReplaceAttachedGameClientAsync(process, cancellationToken)
            .ConfigureAwait(false);

        RegisterProcess(process);

        _logger.LogInformation(
            "PCSX2 process launched (ID: {ProcessId}). DataManager initialization deferred",
            process.Id
        );

        RegisterProcessOutputReader(process, stdOut, stdError, cancellationToken);

        return attachedGameClient;
    }

    public IGameClient? GetActiveGameClient()
    {
        lock (_clientStateLock)
            return _attachedGameClient;
    }

    private async Task HandleProcessOutputAsync(
        Process process,
        ProcessAsyncEnumerable stdOut,
        ProcessAsyncEnumerable stdError,
        CancellationTokenSource outputCts
    )
    {
        WeakReference<CancellationTokenSource> weakCts = new(outputCts);

        process.Exited += ExitHandler;

        try
        {
            Task[] processingTasks = CreateProcessingTasks(stdOut, stdError, outputCts.Token);
            _ = await Task.WhenAny(Task.WhenAll(processingTasks), process.WaitForExitAsync(outputCts.Token))
                .ConfigureAwait(false);
        }
        catch (ProcessErrorException ex)
        {
            HandleProcessError(ex, process.Id);
        }
        catch (OperationCanceledException) when (outputCts.IsCancellationRequested) { }
        finally
        {
            process.Exited -= ExitHandler;
            RemoveProcessOutputRegistration(process.Id, outputCts);
        }

        return;

        void ExitHandler(object? sender, EventArgs e)
        {
            if (!weakCts.TryGetTarget(out CancellationTokenSource? strongCts) || strongCts.IsCancellationRequested)
                return;

            // Guard against the race where process.Exited fires from the thread pool after the
            // 'using' block has already disposed cts (the unsubscription in 'finally' does not
            // prevent callbacks that were already enqueued before unsubscription completed).
            try
            {
                strongCts.Cancel();
            }
            catch (ObjectDisposedException) { }
        }
    }

    private Task[] CreateProcessingTasks(
        ProcessAsyncEnumerable _,
        ProcessAsyncEnumerable stdError,
        CancellationToken ct
    ) => [ProcessOutputAsync(stdError, HandleErrorLog, ct)];

    private static async Task ProcessOutputAsync(
        ProcessAsyncEnumerable output,
        Action<string> handler,
        CancellationToken ct
    )
    {
        await foreach (string item in output.WithCancellation(ct).ConfigureAwait(false))
            handler(item);
    }

    private void HandleProcessError(ProcessErrorException ex, int processId)
    {
        _logger.LogError(
            "{ProcessType} process error (ID: {ProcessId}). ExitCode: {ExitCode}",
            "Client",
            processId,
            ex.ExitCode
        );
    }

    private void HandleErrorLog(string log)
    {
        _logger.LogError("[{Source}] {Log}", "CLIENT", log);
        _processErrors.OnNext(log);
    }

    private void HandleProcessExit(object? sender)
    {
        if (sender is not Process process)
            return;

        _processes.TryRemove(process.Id, out _);
        _clientProcessIds.TryRemove(process.Id, out _);

        IGameClient? exitedGameClient = ClearClientStateIfMonitoredProcessMatches(process);
        exitedGameClient?.Dispose();

        _processUpdate.OnNext(
            new ProcessModel
            {
                IsRunning = false,
                Id = process.Id,
                ExitCode = process.ExitCode,
            }
        );
    }

    public async Task TerminateAsync(int? processId = null)
    {
        bool cancellationStarted = false;

        try
        {
            if (processId.HasValue && _processes.TryGetValue(processId.Value, out Process? process))
            {
                PublishCancellationState(true);
                cancellationStarted = true;

                process.Kill();
                await process.WaitForExitAsync().ConfigureAwait(false);
                return;
            }

            Process[] trackedProcesses = [.. _processes.Values];
            if (trackedProcesses.Length == 0)
                return;

            PublishCancellationState(true);
            cancellationStarted = true;

            foreach (Process proc in trackedProcesses)
            {
                proc.Kill();
                await proc.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            if (cancellationStarted)
                PublishCancellationState(false);
        }
    }

    public async Task KillAsync(int processId)
    {
        Process process = Process.GetProcessById(processId);
        PublishCancellationState(true);

        try
        {
            process.Kill();
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        finally
        {
            PublishCancellationState(false);
        }
    }

    public async Task<IGameClient> AttachAsync(int processId)
    {
        (Process? monitoredProcess, IGameClient? attachedGameClient) = GetClientStateSnapshot();
        int? monitoredProcessId = null;
        try
        {
            monitoredProcessId = monitoredProcess?.Id;
        }
        catch (InvalidOperationException)
        {
            // The Process object exists but has lost its backing OS process (e.g. it was closed/disposed
            // by an external caller). Clear the stale reference so we fall through to fresh attach logic.
            _logger.LogWarning(
                "ClientMonitoredProcess handle invalid for PID {ProcessId}; clearing stale reference",
                processId
            );

            if (monitoredProcess is not null)
            {
                IGameClient? staleGameClient = ClearClientStateIfMonitoredProcessMatches(monitoredProcess);
                staleGameClient?.Dispose();
            }

            monitoredProcess = null;
            attachedGameClient = null;
            monitoredProcessId = null;
        }

        if (monitoredProcessId == processId && monitoredProcess is not null)
        {
            _logger.LogInformation("Already attached to process {ProcessId}", processId);

            if (attachedGameClient is { IsAttached: true })
            {
                // Re-broadcast so late subscribers (e.g. EmbeddedGameViewModel) receive the PID.
                _processUpdate.OnNext(
                    new ProcessModel
                    {
                        IsRunning = true,
                        Id = processId,
                        Name = monitoredProcess.ProcessName,
                        StartTime = monitoredProcess.GetSafeStartTime(),
                    }
                );
                return attachedGameClient;
            }

            IGameClient reAttached = await ReplaceAttachedGameClientAsync(monitoredProcess, CancellationToken.None)
                .ConfigureAwait(false);

            _processUpdate.OnNext(
                new ProcessModel
                {
                    IsRunning = true,
                    Id = processId,
                    Name = monitoredProcess.ProcessName,
                    StartTime = monitoredProcess.GetSafeStartTime(),
                }
            );

            return reAttached;
        }

        Process process = Process.GetProcessById(processId);
        if (process.HasExited)
            throw new InvalidOperationException($"Process {processId} has already exited.");

        IGameClient freshAttached = await ReplaceAttachedGameClientAsync(process, CancellationToken.None)
            .ConfigureAwait(false);

        RegisterProcess(process);

        _logger.LogInformation(
            "Attached to existing process ID: {ProcessId}. DataManager initialization deferred",
            processId
        );
        return freshAttached;
    }

    private async Task<IGameClient> ReplaceAttachedGameClientAsync(Process process, CancellationToken cancellationToken)
    {
        IGameClient newClient = await _gameClientFactory
            .CreateAndAttachGameClientAsync(process, cancellationToken)
            .ConfigureAwait(false);

        IGameClient? previousClient = ReplaceAttachedGameClient(newClient);
        previousClient?.Dispose();

        return newClient;
    }

    public Observable<string> GetErrorObservable() => _processErrors.AsObservable();

    public bool HasExited(int processId) =>
        !_processes.TryGetValue(processId, out Process? process) || process.HasExited;

    public int GetExitCode(int processId) =>
        _processes.TryGetValue(processId, out Process? process) ? process.ExitCode : -1;

    public void Dispose()
    {
        StopProcessOutputReaders();

        foreach (KeyValuePair<int, Process> processEntry in _processes)
        {
            if (_processes.TryRemove(processEntry.Key, out Process? process))
                process.Dispose();
        }

        _clientProcessIds.Clear();

        IGameClient? attachedGameClient = ClearClientState();
        attachedGameClient?.Dispose();

        _isCancelling.Dispose();
        _processErrors.Dispose();
        _processUpdate.Dispose();
    }
}

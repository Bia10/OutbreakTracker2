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
    private readonly ILogger<ProcessLauncher> _logger = logger;
    private readonly IGameClientFactory _gameClientFactory = gameClientFactory;
    private readonly Subject<string> _processErrors = new();
    private readonly Subject<ProcessModel> _processUpdate = new();
    private readonly Subject<bool> _isCancelling = new();
    private readonly ConcurrentDictionary<int, Process> _processes = new();
    private readonly ConcurrentDictionary<int, byte> _clientProcessIds = new();

    public Observable<bool> IsCancelling => _isCancelling.AsObservable();
    public Observable<ProcessModel> ProcessUpdate => _processUpdate.AsObservable();
    public Process? ClientMonitoredProcess { get; private set; }
    public IGameClient? AttachedGameClient { get; private set; }

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

    private void RegisterProcess(Process process)
    {
        process.EnableRaisingEvents = true;
        process.Exited += (sender, _) => HandleProcessExit(sender);

        // TODO: manage multiple instances - for now, only single client monitored
        _processes.TryAdd(process.Id, process);
        _clientProcessIds.TryAdd(process.Id, 0);

        ClientMonitoredProcess = process;

        _processUpdate.OnNext(
            new ProcessModel
            {
                IsRunning = true,
                Id = ClientMonitoredProcess.Id,
                Name = ClientMonitoredProcess.ProcessName,
                StartTime = ClientMonitoredProcess.GetSafeStartTime(),
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

        _ = HandleProcessOutputAsync(process, stdOut, stdError, cancellationToken);

        return attachedGameClient;
    }

    public IGameClient? GetActiveGameClient() => AttachedGameClient;

    private async Task HandleProcessOutputAsync(
        Process process,
        ProcessAsyncEnumerable stdOut,
        ProcessAsyncEnumerable stdError,
        CancellationToken ct
    )
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        WeakReference<CancellationTokenSource> weakCts = new(cts);

        process.Exited += ExitHandler;

        try
        {
            Task[] processingTasks = CreateProcessingTasks(stdOut, stdError, cts.Token);
            _ = await Task.WhenAny(Task.WhenAll(processingTasks), process.WaitForExitAsync(cts.Token))
                .ConfigureAwait(false);
        }
        catch (ProcessErrorException ex)
        {
            HandleProcessError(ex, process.Id);
        }
        finally
        {
            process.Exited -= ExitHandler;
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

        if (ReferenceEquals(ClientMonitoredProcess, process))
            ClientMonitoredProcess = null;

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
        if (processId.HasValue && _processes.TryGetValue(processId.Value, out Process? process))
        {
            process.Kill();
            await process.WaitForExitAsync().ConfigureAwait(false);
            return;
        }

        foreach (Process proc in _processes.Values)
        {
            proc.Kill();
            await proc.WaitForExitAsync().ConfigureAwait(false);
        }
    }

    public async Task KillAsync(int processId)
    {
        Process process = Process.GetProcessById(processId);
        process.Kill();
        await process.WaitForExitAsync().ConfigureAwait(false);
    }

    public async Task<IGameClient> AttachAsync(int processId)
    {
        Process? monitoredProcess = ClientMonitoredProcess;
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
            ClientMonitoredProcess = null;
            monitoredProcess = null;
        }

        if (monitoredProcessId == processId && monitoredProcess is not null)
        {
            _logger.LogInformation("Already attached to process {ProcessId}", processId);

            if (AttachedGameClient is { IsAttached: true })
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
                return AttachedGameClient;
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

        IGameClient? previousClient = AttachedGameClient;
        AttachedGameClient = newClient;
        previousClient?.Dispose();

        return newClient;
    }

    public Observable<string> GetErrorObservable() => _processErrors.AsObservable();

    public bool HasExited(int processId) => !_processes.ContainsKey(processId) || _processes[processId].HasExited;

    public int GetExitCode(int processId) =>
        _processes.TryGetValue(processId, out Process? process) ? process.ExitCode : -1;

    public void Dispose()
    {
        _processErrors.Dispose();

        _processUpdate.Dispose();

        foreach (Process process in _processes.Values)
            process.Dispose();

        AttachedGameClient?.Dispose();
        AttachedGameClient = null;
    }
}

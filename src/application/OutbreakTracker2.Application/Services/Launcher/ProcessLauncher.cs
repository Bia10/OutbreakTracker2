using Cysharp.Diagnostics;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.PCSX2.Client;
using R3;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Services.Launcher;

public class ProcessLauncher : IProcessLauncher, IDisposable
{
    private readonly ILogger<ProcessLauncher> _logger;
    private readonly Subject<string> _processErrors = new();
    private readonly Subject<ProcessModel> _processUpdate = new();
    private readonly Subject<bool> _isCancelling = new();
    private readonly ConcurrentDictionary<int, Process> _processes = new();
    private readonly ConcurrentDictionary<int, byte> _clientProcessIds = new();

    public Observable<bool> IsCancelling => _isCancelling.AsObservable();
    public Observable<ProcessModel> ProcessUpdate => _processUpdate.AsObservable();
    public Process? ClientMonitoredProcess { get; private set; }
    public GameClient? AttachedGameClient { get; private set; }

    public ProcessLauncher(ILogger<ProcessLauncher> logger)
    {
        _logger = logger;
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName, string? arguments)
        => new()
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(fileName)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

    private void RegisterProcess(Process process)
    {
        process.EnableRaisingEvents = true;
        process.Exited += (sender, _) => HandleProcessExit(sender);

        // TODO: manage multiple instances - for now, only single client monitored
        _processes.TryAdd(process.Id, process);
        _clientProcessIds.TryAdd(process.Id, 0);

        ClientMonitoredProcess = process;

        _processUpdate.OnNext(new ProcessModel
        {
            IsRunning = true,
            Id = ClientMonitoredProcess.Id,
            Name = ClientMonitoredProcess.ProcessName,
            StartTime = GetSafeStartTime(ClientMonitoredProcess)
        });
    }

    public Task<GameClient> LaunchAsync(
        string fileName,
        string? arguments,
        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo processStartInfo = CreateProcessStartInfo(fileName, arguments);

        (Process process, ProcessAsyncEnumerable stdOut, ProcessAsyncEnumerable stdError) =
            ProcessX.GetDualAsyncEnumerable(processStartInfo);

        RegisterProcess(process);

        AttachedGameClient = new GameClient();
        AttachedGameClient.Attach(process);

        _logger.LogInformation("PCSX2 process launched (ID: {ProcessId}). DataManager initialization deferred", process.Id);

        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _ = HandleProcessOutputAsync(
            process,
            stdOut,
            stdError,
            cts.Token);

        return Task.FromResult(AttachedGameClient);
    }

    public GameClient? GetActiveGameClient()
        => AttachedGameClient;

    private async Task HandleProcessOutputAsync(
        Process process,
        ProcessAsyncEnumerable stdOut,
        ProcessAsyncEnumerable stdError,
        CancellationToken ct)
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
            if (weakCts.TryGetTarget(out CancellationTokenSource? strongCts)
                && !strongCts.IsCancellationRequested)
                strongCts.Cancel();
        }
    }

    private Task[] CreateProcessingTasks(
        ProcessAsyncEnumerable _,
        ProcessAsyncEnumerable stdError,
        CancellationToken ct)
        =>
        [
            ProcessOutputAsync(stdError, HandleErrorLog, ct),
        ];

    private static async Task ProcessOutputAsync(ProcessAsyncEnumerable output, Action<string> handler, CancellationToken ct)
    {
        await foreach (string item in output.WithCancellation(ct).ConfigureAwait(false))
            handler(item);
    }

    private void HandleProcessError(ProcessErrorException ex, int processId)
    {
        _logger.LogError("{ProcessType} process error (ID: {ProcessId}). ExitCode: {ExitCode}", "Client", processId, ex.ExitCode);
    }

    private void HandleErrorLog(string log)
    {
        _logger.LogError("[{Source}] {Log}", "CLIENT", log);
        _processErrors.OnNext(log);
    }

    private void HandleProcessExit(object? sender)
    {
        if (sender is not Process process) return;

        _processes.TryRemove(process.Id, out _);
        _clientProcessIds.TryRemove(process.Id, out _);

        _processUpdate.OnNext(new ProcessModel
        {
            IsRunning = false,
            Id = process.Id,
            ExitCode = process.ExitCode
        });
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

    private static DateTime GetSafeStartTime(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return DateTime.MinValue;
        }
    }

    public Task<GameClient> AttachAsync(int processId)
    {
        if (ClientMonitoredProcess?.Id == processId)
        {
            _logger.LogInformation("Already attached to process {ProcessId}", processId);

            if (AttachedGameClient is { IsAttached: true })
                return Task.FromResult(AttachedGameClient);

            AttachedGameClient = new GameClient();
            AttachedGameClient.Attach(Process.GetProcessById(processId));

            return Task.FromResult(AttachedGameClient);
        }

        Process process = Process.GetProcessById(processId);
        if (process.HasExited)
            throw new InvalidOperationException("Process has already exited");

        RegisterProcess(process);

        AttachedGameClient?.Dispose();
        AttachedGameClient = new GameClient();
        AttachedGameClient.Attach(process);

        _logger.LogInformation("Attached to existing process ID: {ProcessId}. DataManager initialization deferred", processId);
        return Task.FromResult(AttachedGameClient);
    }

    public Observable<string> GetErrorObservable()
        => _processErrors.AsObservable();

    public bool HasExited(int processId)
        => !_processes.ContainsKey(processId) || _processes[processId].HasExited;

    public int GetExitCode(int processId)
        => _processes.TryGetValue(processId, out Process? process) ? process.ExitCode : -1;

    public void Dispose()
    {
        _processErrors.OnCompleted();
        _processErrors.Dispose();

        _processUpdate.OnCompleted();
        _processUpdate.Dispose();

        foreach (Process process in _processes.Values)
            process.Dispose();

        AttachedGameClient?.Dispose();
        AttachedGameClient = null;
    }
}
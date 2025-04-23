using Cysharp.Diagnostics;
using Microsoft.Extensions.Logging;
using R3;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OutbreakTracker2.App.Services.Data;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.App.Services.ProcessLauncher;

public class ProcessLauncher : IProcessLauncher, IDisposable
{
    private readonly ILogger _logger;
    //private readonly IOutputMonitoringService _outputMonitoringService;

    private readonly Subject<string> _processErrors = new();
    private readonly Subject<ProcessModel> _processUpdate = new();
    private readonly Subject<bool> _isCancelling = new();
    private readonly ConcurrentDictionary<int, Process> _processes = new();
    private readonly ConcurrentDictionary<int, byte> _clientProcessIds = new();

    public Observable<bool> IsCancelling => _isCancelling.AsObservable();
    public Observable<ProcessModel> ProcessUpdate => _processUpdate.AsObservable();
    public Process? ClientMonitoredProcess { get; private set; }
    public GameClient? AttachedGameClient { get; private set; }
    public IDataManager? DataManager { get; private set; }

    public ProcessLauncher(ILogger<ProcessLauncher> logger, IDataManager dataManager)
        // IOutputMonitoringService outputMonitoringService)
    {
        _logger = logger;
        DataManager = dataManager;
        // _outputMonitoringService = outputMonitoringService;
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

        // TODO: manage multiple instances
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

    public Task LaunchAsync(
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
        DataManager?.Initialize(AttachedGameClient);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        return HandleProcessOutputAsync(
            process,
            stdOut,
            stdError,
            cts.Token);
    }

    private async Task HandleProcessOutputAsync(
        Process process,
        ProcessAsyncEnumerable stdOut,
        ProcessAsyncEnumerable stdError,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var weakCts = new WeakReference<CancellationTokenSource>(cts);

        process.Exited += ExitHandler;

        try
        {
            Task[] processingTasks = CreateProcessingTasks(stdOut, stdError, cts.Token);
            await Task.WhenAny(Task.WhenAll(processingTasks), process.WaitForExitAsync(cts.Token))
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
        ProcessAsyncEnumerable stdOut,
        ProcessAsyncEnumerable stdError,
        CancellationToken ct)
        =>
        [
            //ProcessOutputAsync(stdOut, _outputMonitoringService.AddLog, ct),
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
        DataManager = null;

        GC.SuppressFinalize(this);
    }
}

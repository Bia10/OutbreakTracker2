using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.PCSX2.Client;
using R3;

namespace OutbreakTracker2.UnitTests;

public sealed class ProcessLauncherTests
{
    [Test]
    public async Task AttachAsync_UsesGameClientFactory_ForFreshAttach()
    {
        using Process currentProcess = Process.GetCurrentProcess();
        FakeGameClientFactory factory = new();
        using ProcessLauncher launcher = new(NullLogger<ProcessLauncher>.Instance, factory);

        IGameClient attachedClient = await launcher.AttachAsync(currentProcess.Id);

        await Assert.That(factory.CallCount).IsEqualTo(1);
        await Assert.That(factory.LastProcessId).IsEqualTo(currentProcess.Id);
        await Assert.That(ReferenceEquals(attachedClient, factory.LastClient)).IsTrue();
        await Assert.That(ReferenceEquals(launcher.AttachedGameClient, factory.LastClient)).IsTrue();
        await Assert.That(launcher.ClientMonitoredProcess).IsNotNull();
        await Assert.That(launcher.ClientMonitoredProcess!.Id).IsEqualTo(currentProcess.Id);
    }

    [Test]
    public async Task AttachAsync_ReusesExistingGameClient_ForSameTrackedProcess()
    {
        using Process currentProcess = Process.GetCurrentProcess();
        FakeGameClientFactory factory = new();
        using ProcessLauncher launcher = new(NullLogger<ProcessLauncher>.Instance, factory);

        IGameClient firstAttachedClient = await launcher.AttachAsync(currentProcess.Id);
        IGameClient secondAttachedClient = await launcher.AttachAsync(currentProcess.Id);

        await Assert.That(factory.CallCount).IsEqualTo(1);
        await Assert.That(ReferenceEquals(firstAttachedClient, secondAttachedClient)).IsTrue();
        await Assert.That(ReferenceEquals(launcher.AttachedGameClient, firstAttachedClient)).IsTrue();
    }

    [Test]
    public async Task TerminateAsync_PublishesCancellingState_AndClearsTrackedClient()
    {
        using Process childProcess = StartLongRunningShellProcess();
        FakeGameClientFactory factory = new();
        using ProcessLauncher launcher = new(NullLogger<ProcessLauncher>.Instance, factory);
        List<bool> cancellationStates = [];
        using IDisposable subscription = launcher.IsCancelling.Subscribe(onNext: isCancelling =>
            cancellationStates.Add(isCancelling)
        );

        await launcher.AttachAsync(childProcess.Id);
        await launcher.TerminateAsync(childProcess.Id);

        bool stateCleared = SpinWait.SpinUntil(
            () => launcher.ClientMonitoredProcess is null && launcher.AttachedGameClient is null,
            TimeSpan.FromSeconds(2)
        );

        await Assert.That(stateCleared).IsTrue();
        await Assert.That(cancellationStates.Count).IsEqualTo(2);
        await Assert.That(cancellationStates[0]).IsTrue();
        await Assert.That(cancellationStates[1]).IsFalse();
        await Assert.That(factory.LastClient).IsNotNull();
        await Assert.That(factory.LastClient!.IsDisposed).IsTrue();
    }

    private static Process StartLongRunningShellProcess()
    {
        ProcessStartInfo startInfo = OperatingSystem.IsWindows()
            ? CreateWindowsSleepProcessStartInfo()
            : CreatePosixSleepProcessStartInfo();

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the test shell process.");
    }

    private static ProcessStartInfo CreateWindowsSleepProcessStartInfo()
    {
        string? shellPath = Environment.GetEnvironmentVariable("ComSpec");
        if (string.IsNullOrWhiteSpace(shellPath))
            shellPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");

        return new ProcessStartInfo
        {
            FileName = shellPath,
            Arguments = "/c ping -n 6 127.0.0.1 > nul",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
    }

    private static ProcessStartInfo CreatePosixSleepProcessStartInfo() =>
        new()
        {
            FileName = "/bin/sh",
            Arguments = "-c \"sleep 5\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

    private sealed class FakeGameClientFactory : IGameClientFactory
    {
        public int CallCount { get; private set; }

        public int? LastProcessId { get; private set; }

        public FakeGameClient? LastClient { get; private set; }

        public Task<IGameClient> CreateAndAttachGameClientAsync(Process process, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;
            LastProcessId = process.Id;
            LastClient = new FakeGameClient(process);

            return Task.FromResult<IGameClient>(LastClient);
        }
    }

    private sealed class FakeGameClient(Process process) : IGameClient
    {
        public nint Handle => nint.Zero;

        public bool IsAttached => !IsDisposed;

        public Process? Process { get; private set; } = process;

        public bool IsDisposed { get; private set; }

        [SupportedOSPlatform("windows")]
        public nint MainModuleBase => nint.Zero;

        public void Dispose()
        {
            IsDisposed = true;
            Process = null;
        }
    }
}

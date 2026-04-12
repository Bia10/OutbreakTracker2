using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.PCSX2.Client;

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

        public bool IsAttached => true;

        public Process? Process { get; } = process;

        [SupportedOSPlatform("windows")]
        public nint MainModuleBase => nint.Zero;

        public void Dispose() { }
    }
}

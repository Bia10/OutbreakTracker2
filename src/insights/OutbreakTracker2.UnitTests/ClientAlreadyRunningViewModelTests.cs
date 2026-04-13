using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Launcher;
using OutbreakTracker2.Application.Services.Toasts;
using OutbreakTracker2.Application.Views.Dashboard.ClientAlreadyRunning;
using OutbreakTracker2.PCSX2.Client;
using R3;
using SukiUI.Toasts;

namespace OutbreakTracker2.UnitTests;

public sealed class ClientAlreadyRunningViewModelTests
{
    [Test]
    public async Task UpdateProcesses_PopulatesSynchronizedView_ForExistingProcessIds()
    {
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());

        try
        {
            ClientAlreadyRunningViewModel viewModel = new(
                new FakeProcessLauncher(),
                new FakeGameClientConnectionService(),
                new FakeToastService(),
                NullLogger<ClientAlreadyRunningViewModel>.Instance
            );

            using Process currentProcess = Process.GetCurrentProcess();

            viewModel.UpdateProcesses([currentProcess.Id]);

            await Assert.That(viewModel.RunningProcessesView.Count).IsEqualTo(1);
            await Assert.That(viewModel.RunningProcessesView[0].Id).IsEqualTo(currentProcess.Id);
            await Assert.That(viewModel.RunningProcessesView[0].IsRunning).IsTrue();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        private readonly Subject<ProcessModel> _processUpdate = new();
        private readonly Subject<bool> _isCancelling = new();
        private readonly Subject<string> _errors = new();

        public Observable<ProcessModel> ProcessUpdate => _processUpdate;

        public Observable<bool> IsCancelling => _isCancelling;

        public Process? ClientMonitoredProcess => null;

        public IGameClient? AttachedGameClient => null;

        public Task<IGameClient> LaunchAsync(
            string fileName,
            string? arguments,
            CancellationToken cancellationToken = default
        ) => Task.FromException<IGameClient>(new NotSupportedException());

        public Task<IGameClient> AttachAsync(int processId, CancellationToken cancellationToken = default) =>
            Task.FromException<IGameClient>(new NotSupportedException());

        public Task TerminateAsync(int? processId = null) => Task.CompletedTask;

        public Task KillAsync(int processId) => Task.CompletedTask;

        public Observable<string> GetErrorObservable() => _errors;

        public bool HasExited(int processId) => false;

        public int GetExitCode(int processId) => 0;

        public IGameClient? GetActiveGameClient() => null;
    }

    private sealed class FakeGameClientConnectionService : IGameClientConnectionService
    {
        public Task<IGameClient> LaunchAndInitializeAsync(
            string fileName,
            string? arguments,
            CancellationToken cancellationToken = default
        ) => Task.FromException<IGameClient>(new NotSupportedException());

        public Task<IGameClient> AttachAndInitializeAsync(
            int processId,
            CancellationToken cancellationToken = default
        ) => Task.FromException<IGameClient>(new NotSupportedException());
    }

    private sealed class FakeToastService : IToastService
    {
        public Task InvokeInfoToastAsync(string content, string? title = "") => Task.CompletedTask;

        public Task InvokeSuccessToastAsync(string content, string? title = "") => Task.CompletedTask;

        public Task InvokeErrorToastAsync(string content, string? title = "") => Task.CompletedTask;

        public Task InvokeWarningToastAsync(string content, string? title = "") => Task.CompletedTask;

        public ISukiToast CreateToast(string title, object content) => throw new NotSupportedException();

        public ISukiToast CreateInfoToastWithCancelButton(
            string content,
            object cancelButtonContent,
            Action<ISukiToast> onCanceledAction,
            string? title = ""
        ) => throw new NotSupportedException();

        public Task DismissToastAsync(ISukiToast toast) => Task.CompletedTask;
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);

        public override void Send(SendOrPostCallback d, object? state) => d(state);
    }
}

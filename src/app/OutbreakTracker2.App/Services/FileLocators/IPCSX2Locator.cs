using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services.FileLocators;

public interface IPCSX2Locator
{
    public ValueTask<string?> FindExeAsync(TimeSpan timeout = default, CancellationToken ct = default);
}
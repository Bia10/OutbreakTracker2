using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLinq;

namespace OutbreakTracker2.App.Services.FileLocators;

public class PCSX2Locator : IPCSX2Locator
{
    private readonly ILogger<PCSX2Locator> _logger;
    private const string PCSX2FolderName = "PCSX2";
    private const string PCSX2ExeName = "pcsx2-qt.exe";

    public PCSX2Locator(ILogger<PCSX2Locator> logger)
    {
        _logger = logger;
    }

    private static readonly Environment.SpecialFolder[] SpecialFolders =
    [
        Environment.SpecialFolder.ProgramFiles,
        Environment.SpecialFolder.ProgramFilesX86,
        Environment.SpecialFolder.CommonProgramFiles,
        Environment.SpecialFolder.ApplicationData,
        Environment.SpecialFolder.LocalApplicationData
    ];

    public async ValueTask<string?> FindExeAsync(
        TimeSpan timeout = default,
        CancellationToken ct = default)
    {
        if (timeout == TimeSpan.Zero)
            timeout = TimeSpan.FromSeconds(10);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            string? specialPath = CheckSpecialFolders();
            if (specialPath is not null) return specialPath;

            string[] drives = DriveInfo.GetDrives()
                .AsValueEnumerable()
                .Where(driveInfo => driveInfo is { DriveType: DriveType.Fixed, IsReady: true })
                .Select(driveInfo => driveInfo.RootDirectory.FullName)
                .ToArray();

            var resultChannel = Channel.CreateBounded<string>(1);
            IEnumerable<Task> _ = drives.Select(async drive =>
                await ScanDriveAsync(drive, resultChannel.Writer, linkedCts.Token));

            return await await Task.WhenAny(resultChannel.Reader.ReadAsync(linkedCts.Token)
                    .AsTask(), Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token)
                    .ContinueWith(string? (_) => null, TaskContinuationOptions.ExecuteSynchronously)!
            );
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "PCSX2 search timed out after {Timeout}", timeout);
            return null;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "PCSX2 search canceled");
            return null;
        }
        finally
        {
            await linkedCts.CancelAsync();
        }
    }

    private static string? CheckSpecialFolders()
        => (from folder in SpecialFolders.AsValueEnumerable()
                select Environment.GetFolderPath(folder) into basePath
                where !string.IsNullOrEmpty(basePath)
                select Path.Combine(basePath, PCSX2FolderName, PCSX2ExeName))
            .FirstOrDefault(File.Exists);

    private async Task ScanDriveAsync(
        string drivePath,
        ChannelWriter<string> resultWriter,
        CancellationToken ct)
    {
        try
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                BufferSize = 1024 * 64,
                AttributesToSkip = FileAttributes.System | FileAttributes.Hidden
            };

            await Task.Run(() =>
            {
                foreach (string path in Directory.EnumerateFiles(drivePath, PCSX2ExeName, options)
                             .AsValueEnumerable())
                {
                    ct.ThrowIfCancellationRequested();

                    if (!path.Contains(PCSX2FolderName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    resultWriter.TryWrite(path);
                    return;
                }
            }, ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Drive scan operation canceled");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access to drive scan operation failed");
        }
    }
}
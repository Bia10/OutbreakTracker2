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

public class Pcsx2Locator : IPcsx2Locator
{
    private readonly ILogger<Pcsx2Locator> _logger;
    private const string Pcsx2FolderName = "PCSX2";
    private const string Pcsx2ExeName = "pcsx2-qt.exe";
    private const string IsosSubdir = "ISOs";
    private const string File1Name = "Biohazard - Outbreak.iso";
    private const string File2Name = "Biohazard - Outbreak - File 2.iso";

    public Pcsx2Locator(ILogger<Pcsx2Locator> logger)
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

    public ValueTask<string?> FindOutbreakFile1Async(CancellationToken ct = default)
        => LocateIsoFile(File1Name, ct);

    public ValueTask<string?> FindOutbreakFile2Async(CancellationToken ct = default)
        => LocateIsoFile(File2Name, ct);

    private async ValueTask<string?> LocateIsoFile(string fileName, CancellationToken ct)
    {
        try
        {
            string? exePath = await FindExeAsync(ct: ct).ConfigureAwait(false);
            if (exePath is null)
                return await SystemWideIsoSearch(fileName, ct).ConfigureAwait(false);

            string isoPath = Path.Combine(
                Path.GetDirectoryName(exePath)!,
                IsosSubdir,
                fileName
            );

            if (File.Exists(isoPath))
            {
                _logger.LogInformation("Found ISO {FileName} at {Path}", fileName, isoPath);
                return isoPath;
            }

            return await SystemWideIsoSearch(fileName, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locating ISO file {FileName}", fileName);
            return null;
        }
    }

    private async ValueTask<string?> SystemWideIsoSearch(string fileName, CancellationToken ct)
    {
        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
            BufferSize = 1024 * 64,
            AttributesToSkip = FileAttributes.System | FileAttributes.Hidden
        };

        IEnumerable<string> drives = DriveInfo.GetDrives()
            .Where(d => d is { DriveType: DriveType.Fixed, IsReady: true })
            .Select(d => d.RootDirectory.FullName);

        foreach (string drive in drives)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                string? result = await Task.Run(() =>
                    Directory.EnumerateFiles(drive, fileName, options)
                        .FirstOrDefault(File.Exists), ct).ConfigureAwait(false);

                if (result is null)
                    continue;

                _logger.LogInformation("Found ISO {FileName} at {Path} via system search", fileName, result);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search drive {Drive} for ISO {FileName}", drive, fileName);
            }
        }

        _logger.LogWarning("ISO file {FileName} not found in system-wide search", fileName);
        return null;
    }

    public async ValueTask<string?> FindExeAsync(
        TimeSpan timeout = default,
        CancellationToken ct = default)
    {
        if (timeout == TimeSpan.Zero)
            timeout = TimeSpan.FromSeconds(10);

        using CancellationTokenSource timeoutCts = new(timeout);
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        Channel<string> resultChannel = Channel.CreateBounded<string>(1);

        try
        {
            string? specialPath = CheckSpecialFolders();
            if (specialPath is not null) return specialPath;

            string[] drives = DriveInfo.GetDrives()
                .AsValueEnumerable()
                .Where(driveInfo => driveInfo is { DriveType: DriveType.Fixed, IsReady: true })
                .Select(driveInfo => driveInfo.RootDirectory.FullName)
                .ToArray();

            List<Task> scanTasks = drives.Select(drive => ScanDriveAsync(drive, resultChannel.Writer, linkedCts.Token))
                .ToList();

            Task writingCompletion = Task.WhenAll(scanTasks).ContinueWith(_ =>
            {
                resultChannel.Writer.Complete();
            }, linkedCts.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Task<string> channelReadTask = resultChannel.Reader.ReadAsync(linkedCts.Token).AsTask();

            Task completedTask = await Task.WhenAny(channelReadTask, writingCompletion, Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token)).ConfigureAwait(false);

            if (completedTask == channelReadTask)
                return await channelReadTask.ConfigureAwait(false);
            else if (completedTask == writingCompletion)
                return null;
            else
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                return null;
            }
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
            await linkedCts.CancelAsync().ConfigureAwait(false);
            resultChannel.Writer.TryComplete();
        }
    }

    private static string? CheckSpecialFolders()
        => (from folder in SpecialFolders.AsValueEnumerable()
                select Environment.GetFolderPath(folder) into basePath
                where !string.IsNullOrEmpty(basePath)
                select Path.Combine(basePath, Pcsx2FolderName, Pcsx2ExeName))
            .FirstOrDefault(File.Exists);

    private async Task ScanDriveAsync(
        string drivePath,
        ChannelWriter<string> resultWriter,
        CancellationToken ct)
    {
        try
        {
            EnumerationOptions options = new()
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                BufferSize = 1024 * 64,
                AttributesToSkip = FileAttributes.System | FileAttributes.Hidden
            };

            await Task.Run(() =>
            {
                foreach (string path in Directory.EnumerateFiles(drivePath, Pcsx2ExeName, options)
                             .AsValueEnumerable())
                {
                    ct.ThrowIfCancellationRequested();

                    if (!path.Contains(Pcsx2FolderName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    resultWriter.TryWrite(path);
                    return;
                }
            }, ct).ConfigureAwait(false);
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
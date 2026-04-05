using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Conflict.Resolution.Features.Detection;
using AStar.Dev.Conflict.Resolution.Features.Persistence;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Features.DeltaQueries;
using AStar.Dev.Sync.Engine.Features.Activity;
using AStar.Dev.Sync.Engine.Features.Concurrency;
using AStar.Dev.Sync.Engine.Features.DiskSpace;
using AStar.Dev.Sync.Engine.Features.FileTransfer;
using AStar.Dev.Sync.Engine.Features.LocalScanning;
using AStar.Dev.Sync.Engine.Features.ProgressTracking;
using AStar.Dev.Sync.Engine.Features.Resilience;
using AStar.Dev.Sync.Engine.Features.StateTracking;
using AStar.Dev.Sync.Engine.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Sync.Engine.Features.SyncOrchestration;

/// <inheritdoc />
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered via DI in SyncEngineServiceExtensions.")]
internal sealed class SyncEngine(
    SyncGate syncGate,
    ISyncStateStore stateStore,
    ISyncProgressReporter progressReporter,
    IActivityReporter activityReporter,
    IDeltaQueryService deltaQueryService,
    IFileTransferService fileTransferService,
    ILocalFileScanner localFileScanner,
    IDiskSpaceChecker diskSpaceChecker,
    IDbBackupService dbBackupService,
    ExponentialBackoffPolicy backoffPolicy,
    IFileSystem fileSystem,
    IConflictDetector conflictDetector,
    IConflictStore conflictStore,
    ILogger<SyncEngine> logger) : ISyncEngine
{
    private const double DiskSpaceBufferFactor = 1.1;

    /// <inheritdoc />
    public IObservable<SyncProgress> GetProgressStream(string accountId) => progressReporter.GetProgressStream(accountId);

    /// <inheritdoc />
    public async Task<Result<SyncReport, SyncEngineError>> StartSyncAsync(string accountId, bool isFullResync = false, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var hadMultiAccountWarning = syncGate.IsAnyAccountSyncing();

        if (!syncGate.TryBeginSync(accountId))

            return new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.AlreadyRunning());

        if (hadMultiAccountWarning)
            SyncEngineLogMessage.MultiAccountSyncWarning(logger, accountId);

        try
        {
            return await RunSyncAsync(accountId, isFullResync, hadMultiAccountWarning, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await stateStore.SetStateAsync(accountId, SyncAccountState.Interrupted, ct).ConfigureAwait(false);
            throw;
        }
        finally
        {
            syncGate.EndSync(accountId);
        }
    }

    private async Task<Result<SyncReport, SyncEngineError>> RunSyncAsync(string accountId, bool isFullResync, bool hadMultiAccountWarning, CancellationToken ct)
    {
        var startedAt = DateTimeOffset.UtcNow;

        SyncEngineLogMessage.SyncStarted(logger, accountId, isFullResync);

        var backupSucceeded = await dbBackupService.BackupAsync(ct).ConfigureAwait(false);
        if (!backupSucceeded)
            SyncEngineLogMessage.DbBackupFailed(logger, accountId);

        var resumeResult = await CheckForInterruptedSyncAsync(accountId, ct).ConfigureAwait(false);
        if (resumeResult is not null)

            return resumeResult;

        await stateStore.SetStateAsync(accountId, SyncAccountState.Running, ct).ConfigureAwait(false);

        string? storedDeltaToken = null;
        if (!isFullResync)
            storedDeltaToken = await stateStore.GetDeltaTokenAsync(accountId, ct).ConfigureAwait(false);

        var accessToken = string.Empty;

        Result<DeltaQueryResult, DeltaQueryError> deltaResult = default!;
        await backoffPolicy.ExecuteAsync(async innerCt =>
        {
            deltaResult = await deltaQueryService.GetDeltaAsync(accessToken, accountId, storedDeltaToken, innerCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        if (deltaResult is Result<DeltaQueryResult, DeltaQueryError>.Error deltaError)
        {
            if (deltaError.Reason is DeltaTokenExpiredError)
            {
                await stateStore.SetStateAsync(accountId, SyncAccountState.Interrupted, ct).ConfigureAwait(false);

                return new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.FullResyncRequired());
            }

            await stateStore.SetStateAsync(accountId, SyncAccountState.Failed, ct).ConfigureAwait(false);

            return new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.DeltaQueryFailed(deltaError.Reason.Message));
        }

        var queryResult = ((Result<DeltaQueryResult, DeltaQueryError>.Ok)deltaResult).Value;
        var estimatedDownloadBytes = EstimateDownloadBytes(queryResult.Items);
        var diskCheckResult = await CheckDiskSpaceAsync(accountId, estimatedDownloadBytes, ct).ConfigureAwait(false);

        if (diskCheckResult is not null)

            return diskCheckResult;

        var counters = new SyncCounters();

        await foreach (var item in ToAsyncEnumerable(queryResult.Items).WithCancellation(ct).ConfigureAwait(false))
        {
            await ProcessDeltaItemAsync(accountId, item, queryResult.IsFullSync, counters, null, ct).ConfigureAwait(false);

            var checkpoint = SyncCheckpointFactory.Create(accountId, item.Id);
            await stateStore.SaveCheckpointAsync(checkpoint, ct).ConfigureAwait(false);
        }

        await CollectLocalFilesForUploadAsync(accountId, counters, ct).ConfigureAwait(false);

        await stateStore.SaveDeltaTokenAsync(accountId, queryResult.NextDeltaToken, ct).ConfigureAwait(false);
        await stateStore.SetStateAsync(accountId, SyncAccountState.Completed, ct).ConfigureAwait(false);
        await stateStore.ClearCheckpointAsync(accountId, ct).ConfigureAwait(false);

        var report = SyncReportFactory.Create(accountId, startedAt, DateTimeOffset.UtcNow, counters.Downloaded, counters.Uploaded, counters.Skipped, counters.Conflicts, counters.Errors, queryResult.IsFullSync, counters.Skipped > 0, hadMultiAccountWarning);

        SyncEngineLogMessage.SyncCompleted(logger, accountId, counters.Downloaded, counters.Uploaded, counters.Skipped);

        return new Result<SyncReport, SyncEngineError>.Ok(report);
    }

    private async Task ProcessDeltaItemAsync(string accountId, DeltaItem item, bool isFullSync, SyncCounters counters, DateTimeOffset? lastSyncCompletedAt, CancellationToken ct)
    {
        var slots = syncGate.GetTransferSlots(accountId);
        await slots.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (item.ItemType == DeltaItemType.FolderRenamed)
            {
                await ProcessFolderRenameAsync(item, counters).ConfigureAwait(false);

                return;
            }

            var localPath = item.Name ?? item.Id;

            if (item.ItemType == DeltaItemType.Deleted)
            {
                var deleteConflict = await DetectAndPersistConflictAsync(accountId, localPath, item, lastSyncCompletedAt, ct).ConfigureAwait(false);

                if (deleteConflict is null)
                    SyncEngineLogMessage.DestructiveActionPending(logger, accountId, "delete", localPath);

                return;
            }

            if (isFullSync && IsRemoteAndLocalInSync(item))
            {
                SyncEngineLogMessage.FullResyncFileSkipped(logger, localPath);
                counters.Skipped++;
                activityReporter.Report(accountId, ActivityActionType.Skipped, localPath);

                return;
            }

            var conflict = await DetectAndPersistConflictAsync(accountId, localPath, item, lastSyncCompletedAt, ct).ConfigureAwait(false);

            if (conflict is not null)
            {
                counters.Conflicts++;
                activityReporter.Report(accountId, ActivityActionType.ConflictDetected, localPath);

                return;
            }

            var accessToken = string.Empty;
            var downloadResult = await fileTransferService.DownloadAsync(accessToken, item.Id, localPath, null, ct).ConfigureAwait(false);

            if (downloadResult is Result<OneDrive.Client.Features.FileOperations.FileDownloadResult, string>.Ok)
            {
                counters.Downloaded++;
                activityReporter.Report(accountId, ActivityActionType.Downloaded, localPath);
            }
            else
            {
                counters.Errors.Add($"Download failed for {item.Id}");
                activityReporter.Report(accountId, ActivityActionType.Error, localPath);
            }
        }
        finally
        {
            slots.Release();
        }
    }

    private async Task<ConflictRecord?> DetectAndPersistConflictAsync(string accountId, string filePath, DeltaItem item, DateTimeOffset? lastSyncCompletedAt, CancellationToken ct)
    {
        var accountGuid = Guid.TryParse(accountId, out var parsed) ? parsed : Guid.Empty;
        var detectionResult = await conflictDetector.DetectAsync(accountGuid, filePath, item.LastModifiedDateTime, item.Size, item.ItemType == DeltaItemType.Deleted, lastSyncCompletedAt, ct).ConfigureAwait(false);

        if (detectionResult is not Result<ConflictRecord?, ConflictDetectionError>.Ok detectionOk || detectionOk.Value is null)

            return null;

        var conflict = detectionOk.Value;
        await conflictStore.AddAsync(conflict, ct).ConfigureAwait(false);

        return conflict;
    }

    private async Task ProcessFolderRenameAsync(DeltaItem item, SyncCounters counters)
    {
        var oldName = item.PreviousName ?? item.Id;
        var newName = item.Name ?? item.Id;

        try
        {
            fileSystem.Directory.Move(oldName, newName);
            SyncEngineLogMessage.FolderRenamed(logger, oldName, newName);
        }
        catch (IOException ex)
        {
            SyncEngineLogMessage.FolderRenameFailed(logger, oldName, newName, ex.Message);
            counters.Errors.Add($"Folder rename failed: {oldName} → {newName}: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            SyncEngineLogMessage.FolderRenameFailed(logger, oldName, newName, ex.Message);
            counters.Errors.Add($"Folder rename failed (access denied): {oldName} → {newName}: {ex.Message}");
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task CollectLocalFilesForUploadAsync(string accountId, SyncCounters counters, CancellationToken ct)
    {
        var accessToken = string.Empty;

        await foreach (var filePath in localFileScanner.ScanAsync(accountId, ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();

            var uploadResult = await fileTransferService.UploadAsync(accessToken, filePath, accountId, null, ct).ConfigureAwait(false);

            if (uploadResult is Result<OneDrive.Client.Features.FileOperations.FileUploadResult, string>.Ok)
            {
                counters.Uploaded++;
                activityReporter.Report(accountId, ActivityActionType.Uploaded, filePath);
            }
            else
            {
                counters.Errors.Add($"Upload failed for {filePath}");
                activityReporter.Report(accountId, ActivityActionType.Error, filePath);
            }
        }
    }

    private bool IsRemoteAndLocalInSync(DeltaItem item)
    {
        if (item.Size is null || item.LastModifiedDateTime is null || item.Name is null)

            return false;

        if (!fileSystem.File.Exists(item.Name))

            return false;

        var localInfo = fileSystem.FileInfo.New(item.Name);
        var sizeMatches = localInfo.Length == item.Size.Value;
        var timestampMatches = localInfo.LastWriteTimeUtc == item.LastModifiedDateTime.Value.UtcDateTime;

        return sizeMatches && timestampMatches;
    }

    private async Task<Result<SyncReport, SyncEngineError>?> CheckForInterruptedSyncAsync(string accountId, CancellationToken ct)
    {
        var previousState = await stateStore.GetStateAsync(accountId, ct).ConfigureAwait(false);

        if (previousState != SyncAccountState.Interrupted)

            return null;

        var checkpoint = await stateStore.GetCheckpointAsync(accountId, ct).ConfigureAwait(false);

        if (checkpoint is null)
        {
            SyncEngineLogMessage.SyncFailed(logger, accountId, "Interrupted sync checkpoint is corrupt.");

            return new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.ResumeFailed());
        }

        SyncEngineLogMessage.SyncResuming(logger, accountId, checkpoint.LastCompletedFileId);

        return null;
    }

    private async Task<Result<SyncReport, SyncEngineError>?> CheckDiskSpaceAsync(string accountId, long estimatedDownloadBytes, CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var requiredBytes = (long)(estimatedDownloadBytes * DiskSpaceBufferFactor);
        var available = diskSpaceChecker.GetAvailableFreeSpace(accountId);

        if (available >= requiredBytes)

            return null;

        await stateStore.SetStateAsync(accountId, SyncAccountState.Failed, ct).ConfigureAwait(false);

        return new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.InsufficientSpace(available, requiredBytes));
    }

    private static long EstimateDownloadBytes(IReadOnlyList<DeltaItem> items) => items.Count * 1_024_000L;

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private sealed class SyncCounters
    {
        public int Downloaded;
        public int Uploaded;
        public int Skipped;
        public int Conflicts;
        public List<string> Errors { get; } = [];
    }
}

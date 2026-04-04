using Microsoft.Extensions.Logging;

namespace AStar.Dev.Sync.Engine.Infrastructure;

/// <summary>Compile-time log message templates for the sync engine (NF-00).</summary>
public static partial class SyncEngineLogMessage
{
    /// <summary>Logs that a sync run has started for an account.</summary>
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Sync started for account {AccountId} (full re-sync: {IsFullResync}).")]
    public static partial void SyncStarted(ILogger logger, string accountId, bool isFullResync);

    /// <summary>Logs that a sync run completed successfully.</summary>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Sync completed for account {AccountId}. Downloaded: {FilesDownloaded}, Uploaded: {FilesUploaded}, Skipped: {FilesSkipped}.")]
    public static partial void SyncCompleted(ILogger logger, string accountId, int filesDownloaded, int filesUploaded, int filesSkipped);

    /// <summary>Logs a sync failure with a reason.</summary>
    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Sync failed for account {AccountId}: {ErrorMessage}.")]
    public static partial void SyncFailed(ILogger logger, string accountId, string errorMessage);

    /// <summary>Logs that a sync was interrupted before completing.</summary>
    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Sync interrupted for account {AccountId}: {Reason}.")]
    public static partial void SyncInterrupted(ILogger logger, string accountId, string reason);

    /// <summary>Logs that a scheduled sync tick was skipped because a sync is already in progress.</summary>
    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Scheduled sync skipped for account {AccountId} — another sync is already running.")]
    public static partial void SyncPaused(ILogger logger, string accountId);

    /// <summary>Logs that an interrupted sync is being resumed from a checkpoint.</summary>
    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Sync resuming for account {AccountId} from checkpoint {LastCompletedFileId}.")]
    public static partial void SyncResuming(ILogger logger, string accountId, string lastCompletedFileId);

    /// <summary>Logs a successful local folder rename triggered by a delta item.</summary>
    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Folder renamed: {OldPath} -> {NewPath}.")]
    public static partial void FolderRenamed(ILogger logger, string oldPath, string newPath);

    /// <summary>Logs that a local folder rename failed.</summary>
    [LoggerMessage(EventId = 1007, Level = LogLevel.Error, Message = "Folder rename failed: {OldPath} -> {NewPath}. Reason: {Reason}.")]
    public static partial void FolderRenameFailed(ILogger logger, string oldPath, string newPath, string reason);

    /// <summary>Logs that a special file was skipped during local scan.</summary>
    [LoggerMessage(EventId = 1008, Level = LogLevel.Debug, Message = "Skipping file {FilePath} (special file type).")]
    public static partial void SkippedFile(ILogger logger, string filePath);

    /// <summary>Logs that an excluded directory was skipped during local scan.</summary>
    [LoggerMessage(EventId = 1009, Level = LogLevel.Debug, Message = "Skipping directory {DirectoryPath} (excluded directory).")]
    public static partial void SkippedDirectory(ILogger logger, string directoryPath);

    /// <summary>Logs that a file was skipped during full re-sync because local and remote match.</summary>
    [LoggerMessage(EventId = 1010, Level = LogLevel.Information, Message = "Full re-sync skipping {FilePath} — local LastModified and Size match remote.")]
    public static partial void FullResyncFileSkipped(ILogger logger, string filePath);

    /// <summary>Logs a retry attempt after a failure, including the scheduled delay.</summary>
    [LoggerMessage(EventId = 1011, Level = LogLevel.Warning, Message = "Retry {Attempt}/{MaxRetries} after {DelaySeconds}s due to: {Reason}.")]
    public static partial void RetryingAfterDelay(ILogger logger, int attempt, int maxRetries, double delaySeconds, string reason);

    /// <summary>Logs that a DB backup failed; sync will proceed without a backup.</summary>
    [LoggerMessage(EventId = 1012, Level = LogLevel.Warning, Message = "DB backup failed before sync for account {AccountId}. Proceeding without backup.")]
    public static partial void DbBackupFailed(ILogger logger, string accountId);

    /// <summary>Logs the multi-account sync warning for an account (SE-05).</summary>
    [LoggerMessage(EventId = 1013, Level = LogLevel.Warning, Message = "Multi-account sync warning for account {AccountId} — another account is already syncing.")]
    public static partial void MultiAccountSyncWarning(ILogger logger, string accountId);

    /// <summary>Logs that a file matched local and remote and was skipped.</summary>
    [LoggerMessage(EventId = 1014, Level = LogLevel.Information, Message = "File skipped during sync — {FilePath}: local and remote match.")]
    public static partial void FileSkipped(ILogger logger, string filePath);

    /// <summary>Logs a destructive action (delete/rename) before it is executed (NF-04).</summary>
    [LoggerMessage(EventId = 1015, Level = LogLevel.Error, Message = "Destructive action pending for account {AccountId}: {Action} on {Path}.")]
    public static partial void DestructiveActionPending(ILogger logger, string accountId, string action, string path);
}

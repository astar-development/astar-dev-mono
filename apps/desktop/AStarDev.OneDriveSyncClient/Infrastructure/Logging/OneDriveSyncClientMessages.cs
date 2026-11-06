using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Autostart;
using AStarDev.OneDriveSyncClient.Infrastructure.Notifications;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Logging;

/// <summary>
/// Provides extension methods for logging OneDrive Sync Client-specific events, including sync operations, file transfers, and account management.
/// </summary>
[SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Hashing personal values with LogHash.Of is a single SHA-256 of a short string; it must run before the call so no raw value reaches the logger.")]
public static partial class OneDriveSyncClientMessages
{
    // Sync Progress & Pipeline (2000-2099)

    /// <summary>Logs that all sync workers completed normally.</summary>
    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "[Pipeline] All workers completed normally")]
    public static partial void SyncPipelineCompleted(ILogger logger);

    /// <summary>Logs sync pipeline completion with job count.</summary>
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "[Pipeline] Complete — {Done}/{Total} jobs processed")]
    public static partial void SyncPipelineJobsProcessed(ILogger logger, int done, int total);

    /// <summary>Logs final sync progress update.</summary>
    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "[Pipeline] Final progress raised — done={Done} total={Total}")]
    public static partial void SyncPipelineFinalProgress(ILogger logger, int done, int total);

    /// <summary>Logs worker exception during sync pipeline.</summary>
    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "[Pipeline] Worker threw unhandled exception: {Type}")]
    public static partial void SyncPipelineWorkerException(ILogger logger, string type);

    /// <summary>Logs worker processing a sync job.</summary>
    public static void SyncWorkerProcessing(ILogger logger, int id, string jobType, string path) => SyncWorkerProcessingLogged(logger, id, jobType, LogHash.Of(path));

    [LoggerMessage(EventName = "SyncWorkerProcessing", EventId = 2004, Level = LogLevel.Debug, Message = "[Worker {Id}] Processing {JobType} {PathHash}")]
    private static partial void SyncWorkerProcessingLogged(ILogger logger, int id, string jobType, string pathHash);

    /// <summary>Logs worker exception with details.</summary>
    public static void SyncWorkerException(ILogger logger, int id, string type, string path) => SyncWorkerExceptionLogged(logger, id, type, LogHash.Of(path));

    [LoggerMessage(EventName = "SyncWorkerException", EventId = 2005, Level = LogLevel.Error, Message = "[Worker {Id}] EXCEPTION type={Type} path={PathHash}")]
    private static partial void SyncWorkerExceptionLogged(ILogger logger, int id, string type, string pathHash);

    /// <summary>Logs when no handler is registered for a job type.</summary>
    [LoggerMessage(EventId = 2006, Level = LogLevel.Warning, Message = "[Worker {Id}] No handler registered for job type {JobType}")]
    public static partial void SyncWorkerNoHandler(ILogger logger, int id, string jobType);

    /// <summary>Logs when a sync worker job is cancelled and re-queued.</summary>
    public static void SyncWorkerJobCancelledRequeued(ILogger logger, int id, string path) => SyncWorkerJobCancelledRequeuedLogged(logger, id, LogHash.Of(path));

    [LoggerMessage(EventName = "SyncWorkerJobCancelledRequeued", EventId = 2007, Level = LogLevel.Debug, Message = "[Worker {Id}] Job cancelled — re-queued: {PathHash}")]
    private static partial void SyncWorkerJobCancelledRequeuedLogged(ILogger logger, int id, string pathHash);

    /// <summary>Logs the start of pass preparation (drive state, classifications) before enumeration begins.</summary>
    public static void SyncPipelinePreparing(ILogger logger, string accountId) => SyncPipelinePreparingLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "SyncPipelinePreparing", EventId = 2008, Level = LogLevel.Information, Message = "[Pipeline] Preparing sync pass for {AccountIdHash}")]
    private static partial void SyncPipelinePreparingLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs how long a named stage of a sync pass took.</summary>
    public static void SyncPipelineStageTiming(ILogger logger, string stage, string accountId, long elapsedMs) => SyncPipelineStageTimingLogged(logger, stage, LogHash.Of(accountId), elapsedMs);

    [LoggerMessage(EventName = "SyncPipelineStageTiming", EventId = 2009, Level = LogLevel.Information, Message = "[Pipeline] Stage {Stage} for {AccountIdHash} took {ElapsedMs} ms")]
    private static partial void SyncPipelineStageTimingLogged(ILogger logger, string stage, string accountIdHash, long elapsedMs);

    // Sync Service (2100-2199)

    /// <summary>Logs start of account sync.</summary>
    public static void SyncServiceStarting(ILogger logger, string accountId) => SyncServiceStartingLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "SyncServiceStarting", EventId = 2100, Level = LogLevel.Information, Message = "[SyncService] SyncAccountAsync for {AccountIdHash}")]
    private static partial void SyncServiceStartingLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs completion of account sync.</summary>
    public static void SyncServiceComplete(ILogger logger, string accountId) => SyncServiceCompleteLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "SyncServiceComplete", EventId = 2101, Level = LogLevel.Information, Message = "[SyncService] Sync complete for {AccountIdHash}")]
    private static partial void SyncServiceCompleteLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs unhandled error during sync.</summary>
    public static void SyncServiceError(ILogger logger, string accountId, string errorType) => SyncServiceErrorLogged(logger, LogHash.Of(accountId), errorType);

    [LoggerMessage(EventName = "SyncServiceError", EventId = 2102, Level = LogLevel.Error, Message = "[SyncService] Unhandled error syncing {AccountIdHash}: {ErrorType}")]
    private static partial void SyncServiceErrorLogged(ILogger logger, string accountIdHash, string errorType);

    /// <summary>Logs when re-authentication is required during sync.</summary>
    public static void SyncServiceReAuthRequired(ILogger logger, string accountId) => SyncServiceReAuthRequiredLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "SyncServiceReAuthRequired", EventId = 2103, Level = LogLevel.Warning, Message = "[SyncService] Re-authentication required for {AccountIdHash}")]
    private static partial void SyncServiceReAuthRequiredLogged(ILogger logger, string accountIdHash);

    // Sync Scheduler (2200-2299)

    /// <summary>Logs scheduled sync failure.</summary>
    [LoggerMessage(EventId = 2200, Level = LogLevel.Error, Message = "[SyncScheduler] Scheduled sync failed for {Id}: {ErrorType}")]
    public static partial void SyncSchedulerFailed(ILogger logger, string id, string errorType);

    /// <summary>Logs unhandled exception in sync scheduler timer callback.</summary>
    [LoggerMessage(EventId = 2201, Level = LogLevel.Error, Message = "[SyncScheduler] Unhandled exception in timer callback: {ErrorType}")]
    public static partial void SyncSchedulerTimerError(ILogger logger, string errorType);

    /// <summary>Logs fatal error creating timer in sync scheduler.</summary>
    [LoggerMessage(EventId = 2202, Level = LogLevel.Error, Message = "[SyncScheduler] FATAL ERROR creating Timer: {ErrorType}")]
    public static partial void SyncSchedulerTimerFatal(ILogger logger, string errorType);

    /// <summary>Logs trigger called for unknown account.</summary>
    public static void SyncSchedulerUnknownAccount(ILogger logger, string accountId) => SyncSchedulerUnknownAccountLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "SyncSchedulerUnknownAccount", EventId = 2203, Level = LogLevel.Warning, Message = "[SyncScheduler] TriggerAccountAsync called for unknown account {AccountIdHash}")]
    private static partial void SyncSchedulerUnknownAccountLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs sync cancellation requested for an account.</summary>
    public static void SyncSchedulerCancelled(ILogger logger, string accountId) => SyncSchedulerCancelledLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "SyncSchedulerCancelled", EventId = 2204, Level = LogLevel.Information, Message = "[SyncScheduler] Sync cancellation requested for {AccountIdHash}")]
    private static partial void SyncSchedulerCancelledLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs that a sync was skipped because a sync for the same account is already in progress.</summary>
    public static void SyncSchedulerSkippedAlreadyRunning(ILogger logger, string accountId) => SyncSchedulerSkippedAlreadyRunningLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "SyncSchedulerSkippedAlreadyRunning", EventId = 2205, Level = LogLevel.Warning, Message = "[SyncScheduler] Skipping sync for {AccountIdHash} — already in progress")]
    private static partial void SyncSchedulerSkippedAlreadyRunningLogged(ILogger logger, string accountIdHash);

    // Local Change Detection (2300-2399)

    /// <summary>Logs new/modified local files found.</summary>
    public static void LocalChangeDetectorFound(ILogger logger, int count, string path) => LocalChangeDetectorFoundLogged(logger, count, LogHash.Of(path));

    [LoggerMessage(EventName = "LocalChangeDetectorFound", EventId = 2300, Level = LogLevel.Information, Message = "[LocalChangeDetector] Found {Count} local new/modified files under {PathHash}")]
    private static partial void LocalChangeDetectorFoundLogged(ILogger logger, int count, string pathHash);

    /// <summary>Logs error scanning local directory.</summary>
    public static void LocalChangeDetectorError(ILogger logger, string path, string errorType) => LocalChangeDetectorErrorLogged(logger, LogHash.Of(path), errorType);

    [LoggerMessage(EventName = "LocalChangeDetectorError", EventId = 2301, Level = LogLevel.Error, Message = "[LocalChangeDetector] Error scanning {PathHash}: {ErrorType}")]
    private static partial void LocalChangeDetectorErrorLogged(ILogger logger, string pathHash, string errorType);

    /// <summary>Logs access denied when scanning local directory.</summary>
    public static void LocalChangeDetectorAccessDenied(ILogger logger, string path, string error) => LocalChangeDetectorAccessDeniedLogged(logger, LogHash.Of(path), error);

    [LoggerMessage(EventName = "LocalChangeDetectorAccessDenied", EventId = 2302, Level = LogLevel.Warning, Message = "[LocalChangeDetector] Access denied: {PathHash} — {Error}")]
    private static partial void LocalChangeDetectorAccessDeniedLogged(ILogger logger, string pathHash, string error);

    // Local Deletion Detection (2400-2499)

    /// <summary>Logs local file deleted, removing remote.</summary>
    public static void LocalDeletionDetectorDeleted(ILogger logger, string path) => LocalDeletionDetectorDeletedLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "LocalDeletionDetectorDeleted", EventId = 2400, Level = LogLevel.Information, Message = "[LocalDeletionDetector] Local file deleted — removing remote: {PathHash}")]
    private static partial void LocalDeletionDetectorDeletedLogged(ILogger logger, string pathHash);

    public static void LocalDeletionDetectorFolderDeleted(ILogger logger, string path) => LocalDeletionDetectorFolderDeletedLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "LocalDeletionDetectorFolderDeleted", EventId = 2404, Level = LogLevel.Information, Message = "[LocalDeletionDetector] Local folder deleted — removing remote: {PathHash}")]
    private static partial void LocalDeletionDetectorFolderDeletedLogged(ILogger logger, string pathHash);

    /// <summary>Logs deleted remote item.</summary>
    [LoggerMessage(EventId = 2401, Level = LogLevel.Debug, Message = "[LocalDeletionDetector] Deleted remote item {RemoteId}")]
    public static partial void LocalDeletionDetectorRemoteDeleted(ILogger logger, string remoteId);

    /// <summary>Logs failure to delete remote item with an exception.</summary>
    [LoggerMessage(EventId = 2402, Level = LogLevel.Error, Message = "[LocalDeletionDetector] Failed to delete remote item {RemoteId}: {ErrorType}")]
    public static partial void LocalDeletionDetectorDeleteException(ILogger logger, string remoteId, string errorType);

    /// <summary>Logs failure to delete remote item without an exception (API error string only).</summary>
    [LoggerMessage(EventId = 2403, Level = LogLevel.Error, Message = "[LocalDeletionDetector] Failed to delete remote item {RemoteId}: {Error}")]
    public static partial void LocalDeletionDetectorDeleteFailed(ILogger logger, string remoteId, string error);

    // Remote Deletion Detection (2500-2599)

    /// <summary>Logs remote file deleted, removing local.</summary>
    public static void RemoteDeletionDetectorFileDeleted(ILogger logger, string path) => RemoteDeletionDetectorFileDeletedLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "RemoteDeletionDetectorFileDeleted", EventId = 2500, Level = LogLevel.Information, Message = "[RemoteDeletionDetector] Remote file deleted — removing local: {PathHash}")]
    private static partial void RemoteDeletionDetectorFileDeletedLogged(ILogger logger, string pathHash);

    /// <summary>Logs remote folder deleted, removing local.</summary>
    public static void RemoteDeletionDetectorFolderDeleted(ILogger logger, string path) => RemoteDeletionDetectorFolderDeletedLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "RemoteDeletionDetectorFolderDeleted", EventId = 2501, Level = LogLevel.Information, Message = "[RemoteDeletionDetector] Remote folder deleted — removing local: {PathHash}")]
    private static partial void RemoteDeletionDetectorFolderDeletedLogged(ILogger logger, string pathHash);

    /// <summary>Logs remote item no longer present, treating as deleted.</summary>
    public static void RemoteDeletionDetectorNotPresent(ILogger logger, string path) => RemoteDeletionDetectorNotPresentLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "RemoteDeletionDetectorNotPresent", EventId = 2502, Level = LogLevel.Information, Message = "[RemoteDeletionDetector] Remote item no longer present — treating as deleted: {PathHash}")]
    private static partial void RemoteDeletionDetectorNotPresentLogged(ILogger logger, string pathHash);

    // Remote Folder Enumeration (2600-2699)

    /// <summary>Logs enumeration of remote folder.</summary>
    public static void RemoteFolderEnumeratorEnumerating(ILogger logger, string path, string accountId) => RemoteFolderEnumeratorEnumeratingLogged(logger, LogHash.Of(path), LogHash.Of(accountId));

    [LoggerMessage(EventName = "RemoteFolderEnumeratorEnumerating", EventId = 2600, Level = LogLevel.Information, Message = "[RemoteFolderEnumerator] Enumerating {PathHash} for {AccountIdHash}")]
    private static partial void RemoteFolderEnumeratorEnumeratingLogged(ILogger logger, string pathHash, string accountIdHash);

    /// <summary>Logs enumeration completed with item count.</summary>
    public static void RemoteFolderEnumeratorEnumerated(ILogger logger, int count, string path) => RemoteFolderEnumeratorEnumeratedLogged(logger, count, LogHash.Of(path));

    [LoggerMessage(EventName = "RemoteFolderEnumeratorEnumerated", EventId = 2601, Level = LogLevel.Information, Message = "[RemoteFolderEnumerator] Enumerated {Count} items under {PathHash}")]
    private static partial void RemoteFolderEnumeratorEnumeratedLogged(ILogger logger, int count, string pathHash);

    /// <summary>Logs no sync rules configured for account.</summary>
    public static void RemoteFolderEnumeratorNoRules(ILogger logger, string accountId) => RemoteFolderEnumeratorNoRulesLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "RemoteFolderEnumeratorNoRules", EventId = 2602, Level = LogLevel.Information, Message = "[RemoteFolderEnumerator] No sync rules configured for {AccountIdHash} — nothing to sync")]
    private static partial void RemoteFolderEnumeratorNoRulesLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs backfill of RemoteItemId for rule.</summary>
    public static void RemoteFolderEnumeratorBackfilling(ILogger logger, string path) => RemoteFolderEnumeratorBackfillingLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "RemoteFolderEnumeratorBackfilling", EventId = 2603, Level = LogLevel.Debug, Message = "[RemoteFolderEnumerator] Back-filling RemoteItemId for rule {PathHash}")]
    private static partial void RemoteFolderEnumeratorBackfillingLogged(ILogger logger, string pathHash);

    /// <summary>Logs failure to enumerate remote folder.</summary>
    public static void RemoteFolderEnumeratorFailed(ILogger logger, string path, string error) => RemoteFolderEnumeratorFailedLogged(logger, LogHash.Of(path), error);

    [LoggerMessage(EventName = "RemoteFolderEnumeratorFailed", EventId = 2604, Level = LogLevel.Error, Message = "[RemoteFolderEnumerator] Failed to enumerate {PathHash}: {Error}")]
    private static partial void RemoteFolderEnumeratorFailedLogged(ILogger logger, string pathHash, string error);

    /// <summary>Logs generic enumeration error.</summary>
    [LoggerMessage(EventId = 2605, Level = LogLevel.Error, Message = "[RemoteFolderEnumerator] {Error}")]
    public static partial void RemoteFolderEnumeratorError(ILogger logger, string error);

    /// <summary>Logs cannot resolve folder ID for rule.</summary>
    public static void RemoteFolderEnumeratorCannotResolveId(ILogger logger, string path) => RemoteFolderEnumeratorCannotResolveIdLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "RemoteFolderEnumeratorCannotResolveId", EventId = 2606, Level = LogLevel.Warning, Message = "[RemoteFolderEnumerator] Cannot resolve folder ID for rule path {PathHash} — skipping")]
    private static partial void RemoteFolderEnumeratorCannotResolveIdLogged(ILogger logger, string pathHash);

    /// <summary>Logs creation of a missing remote folder for a local-only rule.</summary>
    public static void RemoteFolderCreated(ILogger logger, string path) => RemoteFolderCreatedLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "RemoteFolderCreated", EventId = 2609, Level = LogLevel.Information, Message = "[RemoteFolderCreator] Created remote folder for rule path {PathHash}")]
    private static partial void RemoteFolderCreatedLogged(ILogger logger, string pathHash);

    /// <summary>Logs failure to create a missing remote folder for a local-only rule.</summary>
    public static void RemoteFolderCreateFailed(ILogger logger, string path, string error) => RemoteFolderCreateFailedLogged(logger, LogHash.Of(path), error);

    [LoggerMessage(EventName = "RemoteFolderCreateFailed", EventId = 2610, Level = LogLevel.Warning, Message = "[RemoteFolderCreator] Cannot create remote folder for rule path {PathHash}: {Error}")]
    private static partial void RemoteFolderCreateFailedLogged(ILogger logger, string pathHash, string error);

    /// <summary>Logs failure to resolve the drive before creating missing remote folders.</summary>
    public static void RemoteFolderCreatorDriveUnavailable(ILogger logger, string accountId, string error) => RemoteFolderCreatorDriveUnavailableLogged(logger, LogHash.Of(accountId), error);

    [LoggerMessage(EventName = "RemoteFolderCreatorDriveUnavailable", EventId = 2611, Level = LogLevel.Warning, Message = "[RemoteFolderCreator] Cannot resolve drive for account {AccountIdHash}: {Error}")]
    private static partial void RemoteFolderCreatorDriveUnavailableLogged(ILogger logger, string accountIdHash, string error);

    /// <summary>Logs the start of drive ID resolution against the Graph API.</summary>
    public static void RemoteFolderEnumeratorConnectingToDrive(ILogger logger, string accountId) => RemoteFolderEnumeratorConnectingToDriveLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "RemoteFolderEnumeratorConnectingToDrive", EventId = 2607, Level = LogLevel.Debug, Message = "[RemoteFolderEnumerator] Connecting to drive for {AccountIdHash}")]
    private static partial void RemoteFolderEnumeratorConnectingToDriveLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs the start of folder ID resolution for a sync rule.</summary>
    public static void RemoteFolderEnumeratorResolvingFolder(ILogger logger, string path) => RemoteFolderEnumeratorResolvingFolderLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "RemoteFolderEnumeratorResolvingFolder", EventId = 2608, Level = LogLevel.Debug, Message = "[RemoteFolderEnumerator] Resolving folder id for {PathHash}")]
    private static partial void RemoteFolderEnumeratorResolvingFolderLogged(ILogger logger, string pathHash);

    /// <summary>Logs the remote change gate decision for a sync pass.</summary>
    public static void RemoteChangeGateDecided(ILogger logger, string outcome, string accountId, string reason) => RemoteChangeGateDecidedLogged(logger, outcome, LogHash.Of(accountId), reason);

    [LoggerMessage(EventName = "RemoteChangeGateDecided", EventId = 2612, Level = LogLevel.Information, Message = "[RemoteChangeGate] {Outcome} for {AccountIdHash}: {Reason}")]
    private static partial void RemoteChangeGateDecidedLogged(ILogger logger, string outcome, string accountIdHash, string reason);

    /// <summary>Logs why the Graph delta query could not be used, so the sync falls back to a full walk.</summary>
    public static void RemoteChangeGateDeltaFailed(ILogger logger, string accountId, string error) => RemoteChangeGateDeltaFailedLogged(logger, LogHash.Of(accountId), error);

    [LoggerMessage(EventName = "RemoteChangeGateDeltaFailed", EventId = 2613, Level = LogLevel.Warning, Message = "[RemoteChangeGate] Delta query unavailable for {AccountIdHash}: {Error}")]
    private static partial void RemoteChangeGateDeltaFailedLogged(ILogger logger, string accountIdHash, string error);

    /// <summary>Logs how many remote changes affect the synced scope, with a sample, so a triggered walk can be explained.</summary>
    public static void RemoteChangeGateRelevantChanges(ILogger logger, string accountId, int relevantCount, int totalCount, string changes) => RemoteChangeGateRelevantChangesLogged(logger, LogHash.Of(accountId), relevantCount, totalCount, changes);

    [LoggerMessage(EventName = "RemoteChangeGateRelevantChanges", EventId = 2614, Level = LogLevel.Information, Message = "[RemoteChangeGate] {RelevantCount} of {TotalCount} remote changes affect the synced scope for {AccountIdHash}; first changes: {Changes}")]
    private static partial void RemoteChangeGateRelevantChangesLogged(ILogger logger, string accountIdHash, int relevantCount, int totalCount, string changes);

    /// <summary>Logs a Graph children page that took long to return, which points at throttling or a slow network.</summary>
    public static void GraphFolderEnumeratorSlowPage(ILogger logger, string folderPath, long elapsedMs) => GraphFolderEnumeratorSlowPageLogged(logger, LogHash.Of(folderPath), elapsedMs);

    [LoggerMessage(EventName = "GraphFolderEnumeratorSlowPage", EventId = 2615, Level = LogLevel.Warning, Message = "[GraphFolderEnumerator] Slow children page for {FolderPathHash}: {ElapsedMs} ms")]
    private static partial void GraphFolderEnumeratorSlowPageLogged(ILogger logger, string folderPathHash, long elapsedMs);

    /// <summary>Logs periodic progress of a recursive folder walk.</summary>
    public static void GraphFolderEnumeratorProgress(ILogger logger, string rootPath, int pages, long elapsedMs, string folderPath) => GraphFolderEnumeratorProgressLogged(logger, LogHash.Of(rootPath), pages, elapsedMs, LogHash.Of(folderPath));

    [LoggerMessage(EventName = "GraphFolderEnumeratorProgress", EventId = 2616, Level = LogLevel.Information, Message = "[GraphFolderEnumerator] {RootPathHash}: {Pages} pages fetched in {ElapsedMs} ms, currently in {FolderPathHash}")]
    private static partial void GraphFolderEnumeratorProgressLogged(ILogger logger, string rootPathHash, int pages, long elapsedMs, string folderPathHash);

    /// <summary>Logs the totals for a completed recursive folder walk.</summary>
    public static void GraphFolderEnumeratorCompleted(ILogger logger, string rootPath, int items, int pages, long elapsedMs) => GraphFolderEnumeratorCompletedLogged(logger, LogHash.Of(rootPath), items, pages, elapsedMs);

    [LoggerMessage(EventName = "GraphFolderEnumeratorCompleted", EventId = 2617, Level = LogLevel.Information, Message = "[GraphFolderEnumerator] {RootPathHash} walk complete: {Items} items, {Pages} pages, {ElapsedMs} ms")]
    private static partial void GraphFolderEnumeratorCompletedLogged(ILogger logger, string rootPathHash, int items, int pages, long elapsedMs);

    // Download Operations (2700-2799)

    /// <summary>Logs ETag match, skipping download.</summary>
    public static void DownloadETagMatch(ILogger logger, string path) => DownloadETagMatchLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "DownloadETagMatch", EventId = 2700, Level = LogLevel.Debug, Message = "[DownloadJobBuilder] ETag match — skipping unchanged file {PathHash}")]
    private static partial void DownloadETagMatchLogged(ILogger logger, string pathHash);

    /// <summary>Logs download URL absent, fetching on-demand.</summary>
    public static void DownloadUrlAbsent(ILogger logger, string path) => DownloadUrlAbsentLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "DownloadUrlAbsent", EventId = 2701, Level = LogLevel.Debug, Message = "DownloadUrl absent for {PathHash} — fetching on-demand")]
    private static partial void DownloadUrlAbsentLogged(ILogger logger, string pathHash);

    /// <summary>Logs failure to resolve download URL.</summary>
    public static void DownloadUrlResolveFailed(ILogger logger, string path, string error) => DownloadUrlResolveFailedLogged(logger, LogHash.Of(path), error);

    [LoggerMessage(EventName = "DownloadUrlResolveFailed", EventId = 2702, Level = LogLevel.Error, Message = "Could not resolve download URL for {PathHash}: {Error}")]
    private static partial void DownloadUrlResolveFailedLogged(ILogger logger, string pathHash, string error);

    /// <summary>Logs download failure.</summary>
    public static void DownloadFailed(ILogger logger, string path, string error) => DownloadFailedLogged(logger, LogHash.Of(path), error);

    [LoggerMessage(EventName = "DownloadFailed", EventId = 2703, Level = LogLevel.Error, Message = "Download failed for {PathHash}: {Error}")]
    private static partial void DownloadFailedLogged(ILogger logger, string pathHash, string error);

    /// <summary>Logs HTTP 429 throttle during download with retry details.</summary>
    [LoggerMessage(EventId = 2704, Level = LogLevel.Debug, Message = "[HttpDownloader] 429 received, waiting {Delay:F1}s (attempt {Attempt}/{Max})")]
    public static partial void DownloadThrottled(ILogger logger, double delay, int attempt, int max);

    /// <summary>Logs network error during download with retry details.</summary>
    [LoggerMessage(EventId = 2705, Level = LogLevel.Debug, Message = "[HttpDownloader] Network error, retrying in {Delay:F1}s (attempt {Attempt}/{Max})")]
    public static partial void DownloadNetworkError(ILogger logger, double delay, int attempt, int max);

    /// <summary>Logs download cancelled during 429 backoff wait.</summary>
    [LoggerMessage(EventId = 2706, Level = LogLevel.Debug, Message = "[HttpDownloader] Download cancelled during 429 backoff — attempt {Attempt}/{Max}")]
    public static partial void DownloadCancelledDuringBackoff(ILogger logger, int attempt, int max);

    /// <summary>Logs a failed file move attempt with retry details.</summary>
    public static void DownloadMoveRetrying(ILogger logger, string path, int attempt, int max) => DownloadMoveRetryingLogged(logger, LogHash.Of(path), attempt, max);

    [LoggerMessage(EventName = "DownloadMoveRetrying", EventId = 2707, Level = LogLevel.Debug, Message = "[HttpDownloader] Move to '{PathHash}' failed — retrying (attempt {Attempt}/{Max})")]
    private static partial void DownloadMoveRetryingLogged(ILogger logger, string pathHash, int attempt, int max);

    /// <summary>Logs file move exhausted all retries.</summary>
    public static void DownloadMoveExhausted(ILogger logger, string path, int max, string error) => DownloadMoveExhaustedLogged(logger, LogHash.Of(path), max, error);

    [LoggerMessage(EventName = "DownloadMoveExhausted", EventId = 2708, Level = LogLevel.Warning, Message = "[HttpDownloader] Move to '{PathHash}' failed after {Max} attempts — {Error}")]
    private static partial void DownloadMoveExhaustedLogged(ILogger logger, string pathHash, int max, string error);

    /// <summary>Logs, once, that a download gave up after its retries ran out.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reason">Why the attempts failed, for example "rate limited" or "network error".</param>
    /// <param name="max">The number of retries that were made.</param>
    [LoggerMessage(EventId = 2709, Level = LogLevel.Warning, Message = "[HttpDownloader] Download gave up after {Max} retries: {Reason}")]
    public static partial void DownloadRetriesExhausted(ILogger logger, string reason, int max);

    // Upload Operations (2800-2899)

    /// <summary>Logs file upload completion.</summary>
    public static void UploadCompleted(ILogger logger, string path) => UploadCompletedLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "UploadCompleted", EventId = 2800, Level = LogLevel.Information, Message = "Uploaded {PathHash}")]
    private static partial void UploadCompletedLogged(ILogger logger, string pathHash);

    /// <summary>Logs upload service starting upload.</summary>
    public static void UploadServiceStarting(ILogger logger, string path, double size) => UploadServiceStartingLogged(logger, LogHash.Of(path), size);

    [LoggerMessage(EventName = "UploadServiceStarting", EventId = 2801, Level = LogLevel.Information, Message = "[UploadService] Starting upload: {PathHash} ({Size:F2} MB)")]
    private static partial void UploadServiceStartingLogged(ILogger logger, string pathHash, double size);

    /// <summary>Logs upload service completed upload.</summary>
    public static void UploadServiceCompleted(ILogger logger, string path) => UploadServiceCompletedLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "UploadServiceCompleted", EventId = 2802, Level = LogLevel.Information, Message = "[UploadService] Upload complete: {PathHash}")]
    private static partial void UploadServiceCompletedLogged(ILogger logger, string pathHash);

    /// <summary>Logs upload failure.</summary>
    public static void UploadFailed(ILogger logger, string path, string error) => UploadFailedLogged(logger, LogHash.Of(path), error);

    [LoggerMessage(EventName = "UploadFailed", EventId = 2803, Level = LogLevel.Error, Message = "Upload failed for {PathHash}: {Error}")]
    private static partial void UploadFailedLogged(ILogger logger, string pathHash, string error);

    /// <summary>Logs HTTP 429 throttle during chunked upload.</summary>
    [LoggerMessage(EventId = 2804, Level = LogLevel.Debug, Message = "[UploadService] 429 on chunk {Start}-{End}, waiting {Delay:F1}s (attempt {A}/{Max})")]
    public static partial void UploadChunkThrottled(ILogger logger, long start, long end, double delay, int a, int max);

    /// <summary>Logs network error during chunked upload.</summary>
    [LoggerMessage(EventId = 2805, Level = LogLevel.Debug, Message = "[UploadService] Network error on chunk {Start}-{End}, retrying in {Delay:F1}s (attempt {A}/{Max})")]
    public static partial void UploadChunkNetworkError(ILogger logger, long start, long end, double delay, int a, int max);

    /// <summary>Logs HTTP 412 precondition failure when creating an upload session, before a retry.</summary>
    public static void UploadSessionPreconditionFailed(ILogger logger, string path, double delay, int a, int max) => UploadSessionPreconditionFailedLogged(logger, LogHash.Of(path), delay, a, max);

    [LoggerMessage(EventName = "UploadSessionPreconditionFailed", EventId = 2806, Level = LogLevel.Debug, Message = "[UploadService] 412 creating upload session for {PathHash}, retrying in {Delay:F1}s (attempt {A}/{Max})")]
    private static partial void UploadSessionPreconditionFailedLogged(ILogger logger, string pathHash, double delay, int a, int max);

    /// <summary>Logs, once, that an upload gave up after its retries ran out.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reason">Why the attempts failed, for example "rate limited".</param>
    /// <param name="max">The number of retries that were made.</param>
    [LoggerMessage(EventId = 2807, Level = LogLevel.Warning, Message = "[UploadService] Upload gave up after {Max} retries: {Reason}")]
    public static partial void UploadRetriesExhausted(ILogger logger, string reason, int max);

    // Sync Rules & Items (2900-2999)

    /// <summary>Logs persisting sync rule.</summary>
    public static void RulePersisting(ILogger logger, string ruleType, string path, string accountId) => RulePersistingLogged(logger, ruleType, LogHash.Of(path), LogHash.Of(accountId));

    [LoggerMessage(EventName = "RulePersisting", EventId = 2900, Level = LogLevel.Debug, Message = "[AccountFilesViewModel] Persisting {RuleType} rule for {PathHash} (account {AccountIdHash})")]
    private static partial void RulePersistingLogged(ILogger logger, string ruleType, string pathHash, string accountIdHash);

    /// <summary>Logs file exists locally without SyncedItemEntity.</summary>
    public static void SyncedItemLocalExists(ILogger logger, string path) => SyncedItemLocalExistsLogged(logger, LogHash.Of(path));

    [LoggerMessage(EventName = "SyncedItemLocalExists", EventId = 2901, Level = LogLevel.Debug, Message = "[SyncedItemRegistrar] File exists locally without SyncedItemEntity — treating as synced: {PathHash}")]
    private static partial void SyncedItemLocalExistsLogged(ILogger logger, string pathHash);

    /// <summary>Logs drive ID not available.</summary>
    public static void DriveIdNotAvailable(ILogger logger, string accountId) => DriveIdNotAvailableLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "DriveIdNotAvailable", EventId = 2902, Level = LogLevel.Warning, Message = "[AccountFilesViewModel] Drive ID not available when building folder tree for account {AccountIdHash}")]
    private static partial void DriveIdNotAvailableLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs failure to load root folders.</summary>
    public static void RootFoldersLoadFailed(ILogger logger, string accountId, string error) => RootFoldersLoadFailedLogged(logger, LogHash.Of(accountId), error);

    [LoggerMessage(EventName = "RootFoldersLoadFailed", EventId = 2903, Level = LogLevel.Warning, Message = "[AccountFilesViewModel] Failed to load root folders for account {AccountIdHash}: {Error}")]
    private static partial void RootFoldersLoadFailedLogged(ILogger logger, string accountIdHash, string error);

    /// <summary>Logs failure to persist folder selection.</summary>
    public static void FolderSelectionPersistFailed(ILogger logger, string accountId, string errorType) => FolderSelectionPersistFailedLogged(logger, LogHash.Of(accountId), errorType);

    [LoggerMessage(EventName = "FolderSelectionPersistFailed", EventId = 2904, Level = LogLevel.Error, Message = "[AccountFilesViewModel] Failed to persist folder selection for account {AccountIdHash} — {ErrorType}")]
    private static partial void FolderSelectionPersistFailedLogged(ILogger logger, string accountIdHash, string errorType);

    /// <summary>Logs failure to load folder tree children.</summary>
    public static void FolderChildrenLoadFailed(ILogger logger, string path, string error) => FolderChildrenLoadFailedLogged(logger, LogHash.Of(path), error);

    [LoggerMessage(EventName = "FolderChildrenLoadFailed", EventId = 2905, Level = LogLevel.Warning, Message = "[FolderTreeNodeViewModel] Failed to load children for {PathHash}: {Error}")]
    private static partial void FolderChildrenLoadFailedLogged(ILogger logger, string pathHash, string error);

    /// <summary>Logs refusal to open a path that escapes the OneDrive base directory.</summary>
    public static void FileManagerPathEscapesBase(ILogger logger, string candidatePath) => FileManagerPathEscapesBaseLogged(logger, LogHash.Of(candidatePath));

    [LoggerMessage(EventName = "FileManagerPathEscapesBase", EventId = 2906, Level = LogLevel.Warning, Message = "[AccountFilesViewModel] Refused to open folder — path escapes OneDrive base: {CandidatePathHash}")]
    private static partial void FileManagerPathEscapesBaseLogged(ILogger logger, string candidatePathHash);

    // Conflict Resolution (2950-2999)

    /// <summary>Logs conflict resolution failure to get download URL.</summary>
    public static void ConflictDownloadUrlFailed(ILogger logger, string path, string error) => ConflictDownloadUrlFailedLogged(logger, LogHash.Of(path), error);

    [LoggerMessage(EventName = "ConflictDownloadUrlFailed", EventId = 2950, Level = LogLevel.Error, Message = "[ConflictApplier] Could not resolve download URL for {PathHash}: {Error}")]
    private static partial void ConflictDownloadUrlFailedLogged(ILogger logger, string pathHash, string error);

    /// <summary>Logs conflict resolution download failure.</summary>
    public static void ConflictDownloadFailed(ILogger logger, string path, string error) => ConflictDownloadFailedLogged(logger, LogHash.Of(path), error);

    [LoggerMessage(EventName = "ConflictDownloadFailed", EventId = 2951, Level = LogLevel.Error, Message = "[ConflictApplier] Download failed for {PathHash}: {Error}")]
    private static partial void ConflictDownloadFailedLogged(ILogger logger, string pathHash, string error);

    // Account Operations (3000-3099)

    /// <summary>Logs error completing account onboarding wizard.</summary>
    [LoggerMessage(EventId = 3000, Level = LogLevel.Error, Message = "Error completing account onboarding wizard: {ErrorType}")]
    public static partial void AccountOnboardingWizardError(ILogger logger, string errorType);

    /// <summary>Logs unhandled exception in accounts view model wizard completion.</summary>
    [LoggerMessage(EventId = 3001, Level = LogLevel.Error, Message = "[AccountsViewModel.OnWizardCompletedAsync] Unhandled exception: {ErrorType}")]
    public static partial void AccountsViewModelWizardError(ILogger logger, string errorType);

    /// <summary>Logs failure to set active account.</summary>
    [LoggerMessage(EventId = 3002, Level = LogLevel.Error, Message = "[AccountsViewModel.OnCardSelected] Failed to set active account: {ErrorType}")]
    public static partial void AccountSetActiveFailed(ILogger logger, string errorType);

    /// <summary>Logs error adding account.</summary>
    [LoggerMessage(EventId = 3003, Level = LogLevel.Error, Message = "[MainWindowViewModel.OnAccountAddedAsync] Error: {ErrorType}")]
    public static partial void AccountAddError(ILogger logger, string errorType);

    /// <summary>Logs unhandled exception when account added.</summary>
    [LoggerMessage(EventId = 3004, Level = LogLevel.Error, Message = "[MainWindowViewModel.OnAccountAddedAsync] Unhandled exception: {ErrorType}")]
    public static partial void AccountAddUnhandledError(ILogger logger, string errorType);

    /// <summary>Logs error selecting account.</summary>
    [LoggerMessage(EventId = 3005, Level = LogLevel.Error, Message = "[MainWindowViewModel.OnAccountSelectedAsync] Error: {ErrorType}")]
    public static partial void AccountSelectError(ILogger logger, string errorType);

    /// <summary>Logs unhandled exception when account selected.</summary>
    [LoggerMessage(EventId = 3006, Level = LogLevel.Error, Message = "[MainWindowViewModel.OnAccountSelectedAsync] Unhandled exception: {ErrorType}")]
    public static partial void AccountSelectUnhandledError(ILogger logger, string errorType);

    /// <summary>Logs fatal error during main window initialization.</summary>
    [LoggerMessage(EventId = 3007, Level = LogLevel.Error, Message = "[MainWindowViewModel.InitialiseAsync] FATAL ERROR: {ErrorType}")]
    public static partial void MainWindowInitializeFatal(ILogger logger, string errorType);

    /// <summary>Logs a manual "Sync Now" request for an account no longer present in the repository.</summary>
    public static void SyncNowAccountNotFound(ILogger logger, string accountId) => SyncNowAccountNotFoundLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "SyncNowAccountNotFound", EventId = 3008, Level = LogLevel.Warning, Message = "[DashboardAccountViewModel.SyncNowAsync] Account {AccountIdHash} not found — sync not triggered")]
    private static partial void SyncNowAccountNotFoundLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs an unhandled exception while manually triggering sync for an account.</summary>
    public static void SyncNowFailed(ILogger logger, string accountId, string errorType) => SyncNowFailedLogged(logger, LogHash.Of(accountId), errorType);

    [LoggerMessage(EventName = "SyncNowFailed", EventId = 3009, Level = LogLevel.Error, Message = "[DashboardAccountViewModel.SyncNowAsync] Unhandled exception triggering sync for {AccountIdHash}: {ErrorType}")]
    private static partial void SyncNowFailedLogged(ILogger logger, string accountIdHash, string errorType);

    // Bootstrap & Initialization (3100-3199)

    /// <summary>Logs fatal error during bootstrap.</summary>
    [LoggerMessage(EventId = 3100, Level = LogLevel.Error, Message = "[AppBootstrapper] Fatal error during bootstrap: {ErrorType}")]
    public static partial void BootstrapFatal(ILogger logger, string errorType);

    /// <summary>Logs fatal error during application initialization.</summary>
    [LoggerMessage(EventId = 3101, Level = LogLevel.Error, Message = "[ApplicationInitializer.InitializeAsync] FATAL ERROR: {ErrorType}")]
    public static partial void ApplicationInitializeFatal(ILogger logger, string errorType);

    /// <summary>Logs failure to deserialize settings.</summary>
    public static void SettingsDeserializeFailed(ILogger logger, string path, string errorType) => SettingsDeserializeFailedLogged(logger, LogHash.Of(path), errorType);

    [LoggerMessage(EventName = "SettingsDeserializeFailed", EventId = 3102, Level = LogLevel.Warning, Message = "[SettingsService] Failed to deserialize settings from {PathHash} ({ErrorType}); using defaults")]
    private static partial void SettingsDeserializeFailedLogged(ILogger logger, string pathHash, string errorType);

    /// <summary>Logs token cache failure.</summary>
    [LoggerMessage(EventId = 3103, Level = LogLevel.Warning, Message = "Token cache operation failed: {ErrorType}")]
    public static partial void TokenCacheFailed(ILogger logger, string errorType);

    /// <summary>Logs when MSAL silent token acquisition requires UI interaction, capturing the MSAL error code and classification for diagnosis.</summary>
    [LoggerMessage(EventId = 3104, Level = LogLevel.Warning, Message = "[AuthService] Silent token requires UI interaction: ErrorCode={ErrorCode} Classification={Classification}")]
    public static partial void AuthSilentTokenUiRequired(ILogger logger, string errorCode, string classification);

    /// <summary>Logs a MSAL exception during interactive sign-in; the full exception detail is kept out of the UI-facing error message.</summary>
    [LoggerMessage(EventId = 3105, Level = LogLevel.Warning, Message = "[AuthService] MSAL exception during interactive sign-in: ErrorCode={ErrorCode}")]
    public static partial void AuthInteractiveMsalException(ILogger logger, string errorCode);

    /// <summary>Logs an unexpected exception during interactive sign-in; the full exception detail is kept out of the UI-facing error message.</summary>
    [LoggerMessage(EventId = 3106, Level = LogLevel.Warning, Message = "[AuthService] Unexpected exception during interactive sign-in: {ErrorType}")]
    public static partial void AuthInteractiveUnexpectedException(ILogger logger, string errorType);

    /// <summary>Logs a MSAL exception during silent token refresh; the full exception detail is kept out of the UI-facing error message.</summary>
    [LoggerMessage(EventId = 3107, Level = LogLevel.Warning, Message = "[AuthService] MSAL exception during silent token refresh: ErrorCode={ErrorCode}")]
    public static partial void AuthSilentMsalException(ILogger logger, string errorCode);

    // Quota Refresh (3200-3299)

    /// <summary>Logs failure to acquire a silent token during quota refresh.</summary>
    public static void QuotaRefreshTokenFailed(ILogger logger, string accountId) => QuotaRefreshTokenFailedLogged(logger, LogHash.Of(accountId));

    [LoggerMessage(EventName = "QuotaRefreshTokenFailed", EventId = 3200, Level = LogLevel.Warning, Message = "[QuotaRefreshService] Silent token failed for account {AccountIdHash} — quota not refreshed")]
    private static partial void QuotaRefreshTokenFailedLogged(ILogger logger, string accountIdHash);

    /// <summary>Logs failure to fetch quota from the Graph API.</summary>
    public static void QuotaRefreshFetchFailed(ILogger logger, string accountId, string error) => QuotaRefreshFetchFailedLogged(logger, LogHash.Of(accountId), error);

    [LoggerMessage(EventName = "QuotaRefreshFetchFailed", EventId = 3201, Level = LogLevel.Warning, Message = "[QuotaRefreshService] Graph quota fetch failed for account {AccountIdHash}: {Error}")]
    private static partial void QuotaRefreshFetchFailedLogged(ILogger logger, string accountIdHash, string error);

    /// <summary>Logs a failure recording a completed transfer for the throughput graph — the sample is dropped.</summary>
    public static void ThroughputRecordFailed(ILogger logger, string accountId, string errorType) => ThroughputRecordFailedLogged(logger, LogHash.Of(accountId), errorType);

    [LoggerMessage(EventName = "ThroughputRecordFailed", EventId = 3204, Level = LogLevel.Warning, Message = "[ThroughputRecorder] Could not record throughput for account {AccountIdHash}: {ErrorType}")]
    private static partial void ThroughputRecordFailedLogged(ILogger logger, string accountIdHash, string errorType);

    /// <summary>Logs a failure refreshing the throughput graph — the previous bars stay displayed.</summary>
    [LoggerMessage(EventId = 3205, Level = LogLevel.Warning, Message = "[ThroughputGraphViewModel] Throughput refresh failed: {ErrorType}")]
    public static partial void ThroughputRefreshFailed(ILogger logger, string errorType);

    /// <summary>Logs a failure refreshing the dashboard synced file count for an account — the previous count stays displayed.</summary>
    public static void DashboardFileCountRefreshFailed(ILogger logger, string accountId, string errorType) => DashboardFileCountRefreshFailedLogged(logger, LogHash.Of(accountId), errorType);

    [LoggerMessage(EventName = "DashboardFileCountRefreshFailed", EventId = 3203, Level = LogLevel.Warning, Message = "[DashboardFileCountRefresher] File count refresh failed for account {AccountIdHash}: {ErrorType}")]
    private static partial void DashboardFileCountRefreshFailedLogged(ILogger logger, string accountIdHash, string errorType);

    /// <summary>Logs unexpected exception during the startup quota refresh pass — startup continues normally.</summary>
    [LoggerMessage(EventId = 3202, Level = LogLevel.Warning, Message = "[ApplicationInitializer] Startup quota refresh failed: {ErrorType}")]
    public static partial void QuotaRefreshStartupFailed(ILogger logger, string errorType);

    // Classification Repository (3300-3399)

    /// <summary>Logs that a persisted classification category row failed validation and was skipped during load.</summary>
    [LoggerMessage(EventId = 3300, Level = LogLevel.Warning, Message = "[FileClassificationRepository] Skipping persisted category {EntityId} — validation failed: {Error}")]
    public static partial void ClassificationRowSkipped(ILogger logger, int entityId, string error);

    // Person Category Cleanup (3410-3419)

    /// <summary>Logs how many invalid Person child categories were removed at startup.</summary>
    [LoggerMessage(EventId = 3410, Level = LogLevel.Information, Message = "[PersonCategoryCleanupService] Removed {Count} invalid Person categories")]
    public static partial void PersonCategoryCleanupCompleted(ILogger logger, int count);

    /// <summary>Logs that the startup Person category cleanup failed; startup continues.</summary>
    [LoggerMessage(EventId = 3411, Level = LogLevel.Warning, Message = "[PersonCategoryCleanupService] Cleanup failed: {Error}")]
    public static partial void PersonCategoryCleanupFailed(ILogger logger, string error);

    /// <summary>Logs how many default classification categories were inserted at startup.</summary>
    [LoggerMessage(EventId = 3412, Level = LogLevel.Information, Message = "[DefaultCategorySeedingService] Seeded {Count} default categories")]
    public static partial void DefaultCategoriesSeeded(ILogger logger, int count);

    /// <summary>Logs that seeding the default classification categories failed; startup continues.</summary>
    [LoggerMessage(EventId = 3413, Level = LogLevel.Warning, Message = "[DefaultCategorySeedingService] Seeding failed: {Error}")]
    public static partial void DefaultCategorySeedingFailed(ILogger logger, string error);

    // Classification Data Migration (3400-3499)

    /// <summary>Logs total rows migrated from the legacy classification table to the junction table.</summary>
    [LoggerMessage(EventId = 3400, Level = LogLevel.Information, Message = "[ClassificationDataMigrationService] Migration complete — {Count} rows migrated")]
    public static partial void ClassificationDataMigrated(ILogger logger, int count);

    /// <summary>
    /// Logs failure to add a classification category row during migration, including the category name and error details.
    /// The full exception detail is kept out of the UI-facing error message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The name of the classification category.</param>
    /// <param name="err">The error details.</param>
    /// <exception cref="NotImplementedException"></exception>
    [LoggerMessage(EventId = 3401, Level = LogLevel.Information, Message = "[ClassificationDataMigrationService] Add failed for {Name} with error {Error}")]
    public static partial void ClassificationRowAddFailed(ILogger<FileClassificationRepository> logger, string name, string error);

    /// <summary>
    /// Logs that a classification category row was successfully added during migration, including the category name and ID.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The name of the classification category.</param>
    /// <param name="id">The ID of the classification category.</param>
    /// <param name="level">The Level of the classification category.</param>
    [LoggerMessage(EventId = 3402, Level = LogLevel.Information, Message = "[ClassificationDataMigrationService] Add succeeded for {Name} with ID {Id} and Level: {Level}")]
    public static partial void ClassificationRowAdded(ILogger<FileClassificationExportImportService> logger, string name, int id, int level);

    // Notifications (3500-3599)

    /// <summary>
    /// Logs that an OS notification could not be shown because the platform command failed to start.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="command">The command that failed to start.</param>
    /// <param name="error">The error message.</param>
    /// <param name="ex">The exception.</param>
    [LoggerMessage(EventId = 3500, Level = LogLevel.Warning, Message = "[ProcessNotificationSender] Could not show notification using {Command}: {ErrorType}")]
    public static partial void NotificationFailed(ILogger<ProcessNotificationSender> logger, string command, string errorType);

    // Autostart (3600-3699)

    /// <summary>
    /// Logs that the application was registered to launch at OS startup.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="location">The file or registry value that was written.</param>
    public static void AutostartRegistered(ILogger<AutostartService> logger, string location) => AutostartRegisteredLogged(logger, LogHash.Of(location));

    [LoggerMessage(EventName = "AutostartRegistered", EventId = 3600, Level = LogLevel.Information, Message = "[Autostart] Registered to launch at startup via {LocationHash}")]
    private static partial void AutostartRegisteredLogged(ILogger<AutostartService> logger, string locationHash);

    /// <summary>
    /// Logs that the launch-at-OS-startup registration was removed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="location">The file or registry value that was removed.</param>
    public static void AutostartUnregistered(ILogger<AutostartService> logger, string location) => AutostartUnregisteredLogged(logger, LogHash.Of(location));

    [LoggerMessage(EventName = "AutostartUnregistered", EventId = 3602, Level = LogLevel.Information, Message = "[Autostart] Removed launch at startup registration {LocationHash}")]
    private static partial void AutostartUnregisteredLogged(ILogger<AutostartService> logger, string locationHash);

    /// <summary>
    /// Logs that registering the application to launch at OS startup failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reason">The reason registration failed.</param>
    [LoggerMessage(EventId = 3601, Level = LogLevel.Warning, Message = "[Autostart] Could not register to launch at startup: {Reason}")]
    public static partial void AutostartFailed(ILogger<AutostartService> logger, string reason);

    // Application lifetime & data (3700-3799)

    /// <summary>
    /// Logs that the application terminated because of an exception, recording only the exception type name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorType">The full type name of the exception.</param>
    [LoggerMessage(EventId = 3700, Level = LogLevel.Critical, Message = "Application terminated unexpectedly ({ErrorType})")]
    public static partial void ApplicationTerminatedUnexpectedly(ILogger logger, string errorType);

    /// <summary>
    /// Logs an unhandled application exception, recording only the exception type name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorType">The full type name of the exception.</param>
    [LoggerMessage(EventId = 3701, Level = LogLevel.Critical, Message = "[Unhandled] application exception ({ErrorType})")]
    public static partial void UnhandledApplicationException(ILogger logger, string errorType);

    /// <summary>
    /// Logs that a file classification category could not be deleted, recording only the exception type name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="categoryId">The identifier of the category that could not be deleted.</param>
    /// <param name="errorType">The full type name of the exception.</param>
    [LoggerMessage(EventId = 3702, Level = LogLevel.Error, Message = "Failed to delete category {CategoryId} ({ErrorType})")]
    public static partial void CategoryDeleteFailed(ILogger logger, int categoryId, string errorType);
}

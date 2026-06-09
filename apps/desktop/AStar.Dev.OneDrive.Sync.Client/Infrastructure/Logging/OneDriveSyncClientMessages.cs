using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;

/// <summary>
/// Provides extension methods for logging OneDrive Sync Client-specific events, including sync operations, file transfers, and account management.
/// </summary>
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
    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "[Pipeline] Worker threw unhandled exception: {Type} {Error}")]
    public static partial void SyncPipelineWorkerException(ILogger logger, string type, string error, Exception ex);

    /// <summary>Logs worker processing a sync job.</summary>
    [LoggerMessage(EventId = 2004, Level = LogLevel.Debug, Message = "[Worker {Id}] Processing {JobType} {Path}")]
    public static partial void SyncWorkerProcessing(ILogger logger, int id, string jobType, string path);

    /// <summary>Logs worker exception with details.</summary>
    [LoggerMessage(EventId = 2005, Level = LogLevel.Error, Message = "[Worker {Id}] EXCEPTION type={Type} message={Error} path={Path}")]
    public static partial void SyncWorkerException(ILogger logger, int id, string type, string error, string path, Exception ex);

    /// <summary>Logs when no handler is registered for a job type.</summary>
    [LoggerMessage(EventId = 2006, Level = LogLevel.Warning, Message = "[Worker {Id}] No handler registered for job type {JobType}")]
    public static partial void SyncWorkerNoHandler(ILogger logger, int id, string jobType);

    /// <summary>Logs when a sync worker job is cancelled and re-queued.</summary>
    [LoggerMessage(EventId = 2007, Level = LogLevel.Warning, Message = "[Worker {Id}] Job cancelled — re-queued: {Path}")]
    public static partial void SyncWorkerJobCancelledRequeued(ILogger logger, int id, string path);

    // Sync Service (2100-2199)

    /// <summary>Logs start of account sync.</summary>
    [LoggerMessage(EventId = 2100, Level = LogLevel.Information, Message = "[SyncService] SyncAccountAsync for {Email}")]
    public static partial void SyncServiceStarting(ILogger logger, string email);

    /// <summary>Logs completion of account sync.</summary>
    [LoggerMessage(EventId = 2101, Level = LogLevel.Information, Message = "[SyncService] Sync complete for {Email}")]
    public static partial void SyncServiceComplete(ILogger logger, string email);

    /// <summary>Logs unhandled error during sync.</summary>
    [LoggerMessage(EventId = 2102, Level = LogLevel.Error, Message = "[SyncService] Unhandled error syncing {Email}: {Error}")]
    public static partial void SyncServiceError(ILogger logger, string email, string error, Exception ex);

    /// <summary>Logs when re-authentication is required during sync.</summary>
    [LoggerMessage(EventId = 2103, Level = LogLevel.Warning, Message = "[SyncService] Re-authentication required for {Email}")]
    public static partial void SyncServiceReAuthRequired(ILogger logger, string email);

    // Sync Scheduler (2200-2299)

    /// <summary>Logs scheduled sync failure.</summary>
    [LoggerMessage(EventId = 2200, Level = LogLevel.Error, Message = "[SyncScheduler] Scheduled sync failed for {Id}: {Error}")]
    public static partial void SyncSchedulerFailed(ILogger logger, string id, string error, Exception ex);

    /// <summary>Logs unhandled exception in sync scheduler timer callback.</summary>
    [LoggerMessage(EventId = 2201, Level = LogLevel.Error, Message = "[SyncScheduler] Unhandled exception in timer callback: {Error}")]
    public static partial void SyncSchedulerTimerError(ILogger logger, string error, Exception ex);

    /// <summary>Logs fatal error creating timer in sync scheduler.</summary>
    [LoggerMessage(EventId = 2202, Level = LogLevel.Error, Message = "[SyncScheduler] FATAL ERROR creating Timer: {Error}")]
    public static partial void SyncSchedulerTimerFatal(ILogger logger, string error, Exception ex);

    /// <summary>Logs trigger called for unknown account.</summary>
    [LoggerMessage(EventId = 2203, Level = LogLevel.Warning, Message = "[SyncScheduler] TriggerAccountAsync called for unknown account {AccountId}")]
    public static partial void SyncSchedulerUnknownAccount(ILogger logger, string accountId);

    /// <summary>Logs sync cancellation requested for an account.</summary>
    [LoggerMessage(EventId = 2204, Level = LogLevel.Information, Message = "[SyncScheduler] Sync cancellation requested for {AccountId}")]
    public static partial void SyncSchedulerCancelled(ILogger logger, string accountId);

    /// <summary>Logs that a sync was skipped because a sync for the same account is already in progress.</summary>
    [LoggerMessage(EventId = 2205, Level = LogLevel.Warning, Message = "[SyncScheduler] Skipping sync for {AccountId} — already in progress")]
    public static partial void SyncSchedulerSkippedAlreadyRunning(ILogger logger, string accountId);

    // Local Change Detection (2300-2399)

    /// <summary>Logs new/modified local files found.</summary>
    [LoggerMessage(EventId = 2300, Level = LogLevel.Information, Message = "[LocalChangeDetector] Found {Count} local new/modified files under {Path}")]
    public static partial void LocalChangeDetectorFound(ILogger logger, int count, string path);

    /// <summary>Logs error scanning local directory.</summary>
    [LoggerMessage(EventId = 2301, Level = LogLevel.Error, Message = "[LocalChangeDetector] Error scanning {Path}: {Error}")]
    public static partial void LocalChangeDetectorError(ILogger logger, string path, string error, Exception ex);

    /// <summary>Logs access denied when scanning local directory.</summary>
    [LoggerMessage(EventId = 2302, Level = LogLevel.Warning, Message = "[LocalChangeDetector] Access denied: {Path} — {Error}")]
    public static partial void LocalChangeDetectorAccessDenied(ILogger logger, string path, string error);

    // Local Deletion Detection (2400-2499)

    /// <summary>Logs local file deleted, removing remote.</summary>
    [LoggerMessage(EventId = 2400, Level = LogLevel.Information, Message = "[LocalDeletionDetector] Local file deleted — removing remote: {Path}")]
    public static partial void LocalDeletionDetectorDeleted(ILogger logger, string path);

    /// <summary>Logs deleted remote item.</summary>
    [LoggerMessage(EventId = 2401, Level = LogLevel.Debug, Message = "[LocalDeletionDetector] Deleted remote item {RemoteId}")]
    public static partial void LocalDeletionDetectorRemoteDeleted(ILogger logger, string remoteId);

    /// <summary>Logs failure to delete remote item with an exception.</summary>
    [LoggerMessage(EventId = 2402, Level = LogLevel.Error, Message = "[LocalDeletionDetector] Failed to delete remote item {RemoteId}: {Error}")]
    public static partial void LocalDeletionDetectorDeleteFailed(ILogger logger, string remoteId, string error, Exception ex);

    /// <summary>Logs failure to delete remote item without an exception (API error string only).</summary>
    [LoggerMessage(EventId = 2403, Level = LogLevel.Error, Message = "[LocalDeletionDetector] Failed to delete remote item {RemoteId}: {Error}")]
    public static partial void LocalDeletionDetectorDeleteFailed(ILogger logger, string remoteId, string error);

    // Remote Deletion Detection (2500-2599)

    /// <summary>Logs remote file deleted, removing local.</summary>
    [LoggerMessage(EventId = 2500, Level = LogLevel.Information, Message = "[RemoteDeletionDetector] Remote file deleted — removing local: {Path}")]
    public static partial void RemoteDeletionDetectorFileDeleted(ILogger logger, string path);

    /// <summary>Logs remote folder deleted, removing local.</summary>
    [LoggerMessage(EventId = 2501, Level = LogLevel.Information, Message = "[RemoteDeletionDetector] Remote folder deleted — removing local: {Path}")]
    public static partial void RemoteDeletionDetectorFolderDeleted(ILogger logger, string path);

    /// <summary>Logs remote item no longer present, treating as deleted.</summary>
    [LoggerMessage(EventId = 2502, Level = LogLevel.Information, Message = "[RemoteDeletionDetector] Remote item no longer present — treating as deleted: {Path}")]
    public static partial void RemoteDeletionDetectorNotPresent(ILogger logger, string path);

    // Remote Folder Enumeration (2600-2699)

    /// <summary>Logs enumeration of remote folder.</summary>
    [LoggerMessage(EventId = 2600, Level = LogLevel.Information, Message = "[RemoteFolderEnumerator] Enumerating {Path} for {Email}")]
    public static partial void RemoteFolderEnumeratorEnumerating(ILogger logger, string path, string email);

    /// <summary>Logs enumeration completed with item count.</summary>
    [LoggerMessage(EventId = 2601, Level = LogLevel.Information, Message = "[RemoteFolderEnumerator] Enumerated {Count} items under {Path}")]
    public static partial void RemoteFolderEnumeratorEnumerated(ILogger logger, int count, string path);

    /// <summary>Logs no sync rules configured for account.</summary>
    [LoggerMessage(EventId = 2602, Level = LogLevel.Information, Message = "[RemoteFolderEnumerator] No sync rules configured for {Email} — nothing to sync")]
    public static partial void RemoteFolderEnumeratorNoRules(ILogger logger, string email);

    /// <summary>Logs backfill of RemoteItemId for rule.</summary>
    [LoggerMessage(EventId = 2603, Level = LogLevel.Debug, Message = "[RemoteFolderEnumerator] Back-filling RemoteItemId for rule {Path}")]
    public static partial void RemoteFolderEnumeratorBackfilling(ILogger logger, string path);

    /// <summary>Logs failure to enumerate remote folder.</summary>
    [LoggerMessage(EventId = 2604, Level = LogLevel.Error, Message = "[RemoteFolderEnumerator] Failed to enumerate {Path}: {Error}")]
    public static partial void RemoteFolderEnumeratorFailed(ILogger logger, string path, string error);

    /// <summary>Logs generic enumeration error.</summary>
    [LoggerMessage(EventId = 2605, Level = LogLevel.Error, Message = "[RemoteFolderEnumerator] {Error}")]
    public static partial void RemoteFolderEnumeratorError(ILogger logger, string error);

    /// <summary>Logs cannot resolve folder ID for rule.</summary>
    [LoggerMessage(EventId = 2606, Level = LogLevel.Warning, Message = "[RemoteFolderEnumerator] Cannot resolve folder ID for rule path {Path} — skipping")]
    public static partial void RemoteFolderEnumeratorCannotResolveId(ILogger logger, string path);

    // Download Operations (2700-2799)

    /// <summary>Logs ETag match, skipping download.</summary>
    [LoggerMessage(EventId = 2700, Level = LogLevel.Debug, Message = "[DownloadJobBuilder] ETag match — skipping unchanged file {Path}")]
    public static partial void DownloadETagMatch(ILogger logger, string path);

    /// <summary>Logs download URL absent, fetching on-demand.</summary>
    [LoggerMessage(EventId = 2701, Level = LogLevel.Debug, Message = "DownloadUrl absent for {Path} — fetching on-demand")]
    public static partial void DownloadUrlAbsent(ILogger logger, string path);

    /// <summary>Logs failure to resolve download URL.</summary>
    [LoggerMessage(EventId = 2702, Level = LogLevel.Error, Message = "Could not resolve download URL for {Path}: {Error}")]
    public static partial void DownloadUrlResolveFailed(ILogger logger, string path, string error);

    /// <summary>Logs download failure.</summary>
    [LoggerMessage(EventId = 2703, Level = LogLevel.Error, Message = "Download failed for {Path}: {Error}")]
    public static partial void DownloadFailed(ILogger logger, string path, string error);

    /// <summary>Logs HTTP 429 throttle during download with retry details.</summary>
    [LoggerMessage(EventId = 2704, Level = LogLevel.Warning, Message = "[HttpDownloader] 429 received, waiting {Delay:F1}s (attempt {Attempt}/{Max})")]
    public static partial void DownloadThrottled(ILogger logger, double delay, int attempt, int max);

    /// <summary>Logs network error during download with retry details.</summary>
    [LoggerMessage(EventId = 2705, Level = LogLevel.Warning, Message = "[HttpDownloader] Network error, retrying in {Delay:F1}s (attempt {Attempt}/{Max})")]
    public static partial void DownloadNetworkError(ILogger logger, double delay, int attempt, int max);

    /// <summary>Logs download cancelled during 429 backoff wait.</summary>
    [LoggerMessage(EventId = 2706, Level = LogLevel.Warning, Message = "[HttpDownloader] Download cancelled during 429 backoff — {Url} attempt {Attempt}/{Max}")]
    public static partial void DownloadCancelledDuringBackoff(ILogger logger, string url, int attempt, int max);

    /// <summary>Logs a failed file move attempt with retry details.</summary>
    [LoggerMessage(EventId = 2707, Level = LogLevel.Warning, Message = "[HttpDownloader] Move to '{Path}' failed — retrying (attempt {Attempt}/{Max})")]
    public static partial void DownloadMoveRetrying(ILogger logger, string path, int attempt, int max);

    /// <summary>Logs file move exhausted all retries.</summary>
    [LoggerMessage(EventId = 2708, Level = LogLevel.Error, Message = "[HttpDownloader] Move to '{Path}' failed after {Max} attempts — {Error}")]
    public static partial void DownloadMoveExhausted(ILogger logger, string path, int max, string error);

    // Upload Operations (2800-2899)

    /// <summary>Logs file upload completion.</summary>
    [LoggerMessage(EventId = 2800, Level = LogLevel.Information, Message = "Uploaded {Path}")]
    public static partial void UploadCompleted(ILogger logger, string path);

    /// <summary>Logs upload service starting upload.</summary>
    [LoggerMessage(EventId = 2801, Level = LogLevel.Information, Message = "[UploadService] Starting upload: {Path} ({Size:F2} MB)")]
    public static partial void UploadServiceStarting(ILogger logger, string path, double size);

    /// <summary>Logs upload service completed upload.</summary>
    [LoggerMessage(EventId = 2802, Level = LogLevel.Information, Message = "[UploadService] Upload complete: {Path}")]
    public static partial void UploadServiceCompleted(ILogger logger, string path);

    /// <summary>Logs upload failure.</summary>
    [LoggerMessage(EventId = 2803, Level = LogLevel.Error, Message = "Upload failed for {Path}: {Error}")]
    public static partial void UploadFailed(ILogger logger, string path, string error);

    /// <summary>Logs HTTP 429 throttle during chunked upload.</summary>
    [LoggerMessage(EventId = 2804, Level = LogLevel.Warning, Message = "[UploadService] 429 on chunk {Start}-{End}, waiting {Delay:F1}s (attempt {A}/{Max})")]
    public static partial void UploadChunkThrottled(ILogger logger, long start, long end, double delay, int a, int max);

    /// <summary>Logs network error during chunked upload.</summary>
    [LoggerMessage(EventId = 2805, Level = LogLevel.Warning, Message = "[UploadService] Network error on chunk {Start}-{End}, retrying in {Delay:F1}s (attempt {A}/{Max})")]
    public static partial void UploadChunkNetworkError(ILogger logger, long start, long end, double delay, int a, int max);

    // Sync Rules & Items (2900-2999)

    /// <summary>Logs persisting sync rule.</summary>
    [LoggerMessage(EventId = 2900, Level = LogLevel.Debug, Message = "[AccountFilesViewModel] Persisting {RuleType} rule for {Path} (account {AccountId})")]
    public static partial void RulePersisting(ILogger logger, string ruleType, string path, string accountId);

    /// <summary>Logs file exists locally without SyncedItemEntity.</summary>
    [LoggerMessage(EventId = 2901, Level = LogLevel.Debug, Message = "[SyncedItemRegistrar] File exists locally without SyncedItemEntity — treating as synced: {Path}")]
    public static partial void SyncedItemLocalExists(ILogger logger, string path);

    /// <summary>Logs drive ID not available.</summary>
    [LoggerMessage(EventId = 2902, Level = LogLevel.Warning, Message = "[AccountFilesViewModel] Drive ID not available when building folder tree for account {AccountId}")]
    public static partial void DriveIdNotAvailable(ILogger logger, string accountId);

    /// <summary>Logs failure to load root folders.</summary>
    [LoggerMessage(EventId = 2903, Level = LogLevel.Warning, Message = "[AccountFilesViewModel] Failed to load root folders for account {AccountId}: {Error}")]
    public static partial void RootFoldersLoadFailed(ILogger logger, string accountId, string error);

    /// <summary>Logs failure to persist folder selection.</summary>
    [LoggerMessage(EventId = 2904, Level = LogLevel.Error, Message = "[AccountFilesViewModel] Failed to persist folder selection for account {AccountId} — {Error}")]
    public static partial void FolderSelectionPersistFailed(ILogger logger, string accountId, string error, Exception ex);

    /// <summary>Logs failure to load folder tree children.</summary>
    [LoggerMessage(EventId = 2905, Level = LogLevel.Warning, Message = "[FolderTreeNodeViewModel] Failed to load children for {Path}: {Error}")]
    public static partial void FolderChildrenLoadFailed(ILogger logger, string path, string error);

    // Conflict Resolution (2950-2999)

    /// <summary>Logs conflict resolution failure to get download URL.</summary>
    [LoggerMessage(EventId = 2950, Level = LogLevel.Error, Message = "[ConflictApplier] Could not resolve download URL for {Path}: {Error}")]
    public static partial void ConflictDownloadUrlFailed(ILogger logger, string path, string error);

    /// <summary>Logs conflict resolution download failure.</summary>
    [LoggerMessage(EventId = 2951, Level = LogLevel.Error, Message = "[ConflictApplier] Download failed for {Path}: {Error}")]
    public static partial void ConflictDownloadFailed(ILogger logger, string path, string error);

    // Account Operations (3000-3099)

    /// <summary>Logs error completing account onboarding wizard.</summary>
    [LoggerMessage(EventId = 3000, Level = LogLevel.Error, Message = "Error completing account onboarding wizard")]
    public static partial void AccountOnboardingWizardError(ILogger logger, Exception error);

    /// <summary>Logs unhandled exception in accounts view model wizard completion.</summary>
    [LoggerMessage(EventId = 3001, Level = LogLevel.Error, Message = "[AccountsViewModel.OnWizardCompletedAsync] Unhandled exception: {Error}")]
    public static partial void AccountsViewModelWizardError(ILogger logger, string error, Exception ex);

    /// <summary>Logs failure to set active account.</summary>
    [LoggerMessage(EventId = 3002, Level = LogLevel.Error, Message = "[AccountsViewModel.OnCardSelected] Failed to set active account: {Error}")]
    public static partial void AccountSetActiveFailed(ILogger logger, string error, Exception ex);

    /// <summary>Logs error adding account.</summary>
    [LoggerMessage(EventId = 3003, Level = LogLevel.Error, Message = "[MainWindowViewModel.OnAccountAddedAsync] Error: {Error}")]
    public static partial void AccountAddError(ILogger logger, string error, Exception ex);

    /// <summary>Logs unhandled exception when account added.</summary>
    [LoggerMessage(EventId = 3004, Level = LogLevel.Error, Message = "[MainWindowViewModel.OnAccountAddedAsync] Unhandled exception: {Error}")]
    public static partial void AccountAddUnhandledError(ILogger logger, string error, Exception ex);

    /// <summary>Logs error selecting account.</summary>
    [LoggerMessage(EventId = 3005, Level = LogLevel.Error, Message = "[MainWindowViewModel.OnAccountSelectedAsync] Error: {Error}")]
    public static partial void AccountSelectError(ILogger logger, string error, Exception ex);

    /// <summary>Logs unhandled exception when account selected.</summary>
    [LoggerMessage(EventId = 3006, Level = LogLevel.Error, Message = "[MainWindowViewModel.OnAccountSelectedAsync] Unhandled exception: {Error}")]
    public static partial void AccountSelectUnhandledError(ILogger logger, string error, Exception ex);

    /// <summary>Logs fatal error during main window initialization.</summary>
    [LoggerMessage(EventId = 3007, Level = LogLevel.Error, Message = "[MainWindowViewModel.InitialiseAsync] FATAL ERROR: {Error}")]
    public static partial void MainWindowInitializeFatal(ILogger logger, string error, Exception ex);

    // Bootstrap & Initialization (3100-3199)

    /// <summary>Logs fatal error during bootstrap.</summary>
    [LoggerMessage(EventId = 3100, Level = LogLevel.Error, Message = "[AppBootstrapper] Fatal error during bootstrap: {Message}")]
    public static partial void BootstrapFatal(ILogger logger, string message, Exception ex);

    /// <summary>Logs fatal error during application initialization.</summary>
    [LoggerMessage(EventId = 3101, Level = LogLevel.Error, Message = "[ApplicationInitializer.InitializeAsync] FATAL ERROR: {Error}")]
    public static partial void ApplicationInitializeFatal(ILogger logger, string error, Exception ex);

    /// <summary>Logs failure to deserialize settings.</summary>
    [LoggerMessage(EventId = 3102, Level = LogLevel.Warning, Message = "[SettingsService] Failed to deserialize settings from {Path}; using defaults")]
    public static partial void SettingsDeserializeFailed(ILogger logger, string path, Exception ex);

    /// <summary>Logs token cache failure.</summary>
    [LoggerMessage(EventId = 3103, Level = LogLevel.Warning, Message = "Token cache operation failed")]
    public static partial void TokenCacheFailed(ILogger logger, Exception ex);

    /// <summary>Logs when MSAL silent token acquisition requires UI interaction, capturing the MSAL error code and classification for diagnosis.</summary>
    [LoggerMessage(EventId = 3104, Level = LogLevel.Warning, Message = "[AuthService] Silent token requires UI interaction: ErrorCode={ErrorCode} Classification={Classification}")]
    public static partial void AuthSilentTokenUiRequired(ILogger logger, string errorCode, string classification);

    // Quota Refresh (3200-3299)

    /// <summary>Logs failure to acquire a silent token during quota refresh.</summary>
    [LoggerMessage(EventId = 3200, Level = LogLevel.Warning, Message = "[QuotaRefreshService] Silent token failed for account {AccountId} — quota not refreshed")]
    public static partial void QuotaRefreshTokenFailed(ILogger logger, string accountId);

    /// <summary>Logs failure to fetch quota from the Graph API.</summary>
    [LoggerMessage(EventId = 3201, Level = LogLevel.Warning, Message = "[QuotaRefreshService] Graph quota fetch failed for account {AccountId}: {Error}")]
    public static partial void QuotaRefreshFetchFailed(ILogger logger, string accountId, string error);

    /// <summary>Logs unexpected exception during the startup quota refresh pass — startup continues normally.</summary>
    [LoggerMessage(EventId = 3202, Level = LogLevel.Warning, Message = "[ApplicationInitializer] Startup quota refresh failed: {Error}")]
    public static partial void QuotaRefreshStartupFailed(ILogger logger, string error, Exception ex);

    // Application Lifecycle (use ApplicationMessages for these)
    // - Starting/Stopping handled by ApplicationMessages.Starting/Stopping
    // - Unhandled exception handled by MainWindowInitializeFatal, BootstrapFatal, etc.
}

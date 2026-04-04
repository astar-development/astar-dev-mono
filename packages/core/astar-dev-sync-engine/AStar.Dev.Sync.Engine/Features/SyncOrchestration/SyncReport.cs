namespace AStar.Dev.Sync.Engine.Features.SyncOrchestration;

/// <summary>Summary of a completed sync run for an account.</summary>
public sealed record SyncReport(
    string AccountId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    int FilesDownloaded,
    int FilesUploaded,
    int FilesSkipped,
    int ConflictsDetected,
    bool HasErrors,
    IReadOnlyList<string> ErrorMessages,
    bool WasFullResyncTriggered,
    bool HasSkippedFiles,
    bool HadMultiAccountWarning);

/// <summary>Factory for <see cref="SyncReport"/>.</summary>
public static class SyncReportFactory
{
    /// <summary>Creates a <see cref="SyncReport"/>.</summary>
    public static SyncReport Create(string accountId, DateTimeOffset startedAt, DateTimeOffset completedAt, int filesDownloaded, int filesUploaded, int filesSkipped, int conflictsDetected, IReadOnlyList<string> errorMessages, bool wasFullResyncTriggered, bool hasSkippedFiles, bool hadMultiAccountWarning)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(errorMessages);

        return new SyncReport(accountId, startedAt, completedAt, filesDownloaded, filesUploaded, filesSkipped, conflictsDetected, errorMessages.Count > 0, errorMessages, wasFullResyncTriggered, hasSkippedFiles, hadMultiAccountWarning);
    }
}

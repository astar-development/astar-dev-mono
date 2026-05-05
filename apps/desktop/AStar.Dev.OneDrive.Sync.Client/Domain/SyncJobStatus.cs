namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Identity and execution state of a sync job.</summary>
public sealed record SyncJobStatus(Guid Id, DateTimeOffset QueuedAt, SyncJobState State = SyncJobState.Queued, string? ErrorMessage = null, DateTimeOffset? CompletedAt = null);

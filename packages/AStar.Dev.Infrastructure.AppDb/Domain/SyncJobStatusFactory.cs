namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="SyncJobStatus"/>.</summary>
public static class SyncJobStatusFactory
{
    /// <summary>Creates a new <see cref="SyncJobStatus"/> with a generated <see cref="SyncJobStatus.Id"/> and <see cref="SyncJobStatus.QueuedAt"/> timestamp.</summary>
    public static SyncJobStatus Create() => new(Guid.NewGuid(), DateTimeOffset.UtcNow);
}

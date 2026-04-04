namespace AStar.Dev.Sync.Engine.Features.StateTracking;

/// <summary>Records the last successfully processed file so an interrupted sync can be resumed (EH-05).</summary>
public sealed record SyncCheckpoint(string AccountId, string LastCompletedFileId, DateTimeOffset CheckpointedAt);

/// <summary>Factory for <see cref="SyncCheckpoint"/>.</summary>
public static class SyncCheckpointFactory
{
    /// <summary>Creates a <see cref="SyncCheckpoint"/> stamped with the current UTC time.</summary>
    public static SyncCheckpoint Create(string accountId, string lastCompletedFileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastCompletedFileId);

        return new SyncCheckpoint(accountId, lastCompletedFileId, DateTimeOffset.UtcNow);
    }
}

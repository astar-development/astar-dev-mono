namespace AStar.Dev.Sync.Engine.Features.ProgressTracking;

/// <summary>A snapshot of sync progress for a single account at a point in time (SE-13, SE-14).</summary>
public sealed record SyncProgress(string AccountId, int PercentComplete, int EtaSeconds, int FilesProcessed, int FilesTotal);

/// <summary>Factory for <see cref="SyncProgress"/>.</summary>
public static class SyncProgressFactory
{
    /// <summary>Creates a <see cref="SyncProgress"/> snapshot.</summary>
    public static SyncProgress Create(string accountId, int filesProcessed, int filesTotal, int etaSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var percentComplete = filesTotal == 0 ? 0 : (int)Math.Round(filesProcessed * 100.0 / filesTotal);

        return new SyncProgress(accountId, percentComplete, etaSeconds, filesProcessed, filesTotal);
    }
}

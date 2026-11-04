namespace AStarDev.CloudSync.Core.Common;

/// <summary>The sync state of an account or sync pair.</summary>
public abstract record SyncStatus
{
    private SyncStatus()
    {
    }

    /// <summary>Syncing, with the percentage of the current pass completed.</summary>
    public sealed record Syncing(int PercentComplete) : SyncStatus;

    /// <summary>Fully in sync.</summary>
    public sealed record UpToDate : SyncStatus;

    /// <summary>In sync apart from conflicts that need the user's decision.</summary>
    public sealed record HasConflicts(int Count) : SyncStatus;

    /// <summary>Paused by the user.</summary>
    public sealed record Paused : SyncStatus;
}

namespace AStar.Dev.Sync.Engine.Features.StateTracking;

/// <summary>The lifecycle state of a sync run for a single account (EH-04, EH-05, EH-06).</summary>
public enum SyncAccountState
{
    /// <summary>A sync is currently in progress.</summary>
    Running,

    /// <summary>The sync was interrupted (network failure, app shutdown) before completing.</summary>
    Interrupted,

    /// <summary>The sync completed successfully.</summary>
    Completed,

    /// <summary>The sync failed with an unrecoverable error.</summary>
    Failed,
}

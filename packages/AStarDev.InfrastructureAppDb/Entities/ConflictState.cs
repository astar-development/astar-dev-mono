namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents the state of a synchronization conflict for a file during the sync process. This can be used to track whether a conflict is pending resolution, has been resolved, or was skipped based on the configured conflict policy.
/// </summary>
public enum ConflictState
{
    /// <summary>No conflict has been detected.</summary>
    NoConflict,

    /// <summary>A conflict has been detected and is awaiting resolution.</summary>
    Pending,

    /// <summary>The conflict has been resolved.</summary>
    Resolved,

    /// <summary>The conflict was skipped based on the configured conflict policy.</summary>
    Skipped
}

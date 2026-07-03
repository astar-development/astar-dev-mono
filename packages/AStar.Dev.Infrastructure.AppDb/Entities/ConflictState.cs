namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents the state of a synchronization conflict for a file during the sync process. This can be used to track whether a conflict is pending resolution, has been resolved, or was skipped based on the configured conflict policy.
/// </summary>
public enum ConflictState { NoConflict, Pending, Resolved, Skipped }

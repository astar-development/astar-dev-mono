using System;

namespace AStar.Dev.Conflict.Resolution.Domain;

/// <summary>Event raised when the conflict queue is modified (conflict added or resolved).</summary>
public sealed record ConflictQueueChanged(Guid ConflictId, ConflictQueueChangeType ChangeType, int PendingCount);

/// <summary>The type of change that occurred on the conflict queue.</summary>
public enum ConflictQueueChangeType
{
    /// <summary>A new conflict was added to the queue.</summary>
    ConflictAdded,

    /// <summary>A conflict was resolved and removed from the pending queue.</summary>
    ConflictResolved
}

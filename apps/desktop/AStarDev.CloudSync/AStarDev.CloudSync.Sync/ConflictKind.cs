namespace AStarDev.CloudSync.Sync;

/// <summary>Why a path needs the user's decision before the engine may touch it (CR-01, #1405).</summary>
public abstract record ConflictKind
{
    private ConflictKind()
    {
    }

    /// <summary>Changed locally and in the cloud since the baseline.</summary>
    public sealed record BothModified : ConflictKind
    {
        internal BothModified()
        {
        }
    }

    /// <summary>Edited locally, deleted in the cloud.</summary>
    public sealed record LocalModifiedRemoteDeleted : ConflictKind
    {
        internal LocalModifiedRemoteDeleted()
        {
        }
    }

    /// <summary>Deleted locally, edited in the cloud.</summary>
    public sealed record LocalDeletedRemoteModified : ConflictKind
    {
        internal LocalDeletedRemoteModified()
        {
        }
    }

    /// <summary>A new item at the same path on both sides, with no baseline.</summary>
    public sealed record BothAdded : ConflictKind
    {
        internal BothAdded()
        {
        }
    }

    /// <summary>A move whose target path is already in use on the other side.</summary>
    public sealed record MoveTargetTaken : ConflictKind
    {
        internal MoveTargetTaken()
        {
        }
    }

    /// <summary>A 412 precondition failure, or the target changed before the move.</summary>
    public sealed record RemoteChangedDuringTransfer : ConflictKind
    {
        internal RemoteChangedDuringTransfer()
        {
        }
    }
}

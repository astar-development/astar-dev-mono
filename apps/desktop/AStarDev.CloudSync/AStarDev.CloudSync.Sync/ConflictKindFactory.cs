namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ConflictKind"/> cases.</summary>
public static class ConflictKindFactory
{
    /// <summary>Creates the <see cref="ConflictKind.BothModified"/> case.</summary>
    public static ConflictKind CreateBothModified() => new ConflictKind.BothModified();

    /// <summary>Creates the <see cref="ConflictKind.LocalModifiedRemoteDeleted"/> case.</summary>
    public static ConflictKind CreateLocalModifiedRemoteDeleted() => new ConflictKind.LocalModifiedRemoteDeleted();

    /// <summary>Creates the <see cref="ConflictKind.LocalDeletedRemoteModified"/> case.</summary>
    public static ConflictKind CreateLocalDeletedRemoteModified() => new ConflictKind.LocalDeletedRemoteModified();

    /// <summary>Creates the <see cref="ConflictKind.BothAdded"/> case.</summary>
    public static ConflictKind CreateBothAdded() => new ConflictKind.BothAdded();

    /// <summary>Creates the <see cref="ConflictKind.MoveTargetTaken"/> case.</summary>
    public static ConflictKind CreateMoveTargetTaken() => new ConflictKind.MoveTargetTaken();

    /// <summary>Creates the <see cref="ConflictKind.RemoteChangedDuringTransfer"/> case.</summary>
    public static ConflictKind CreateRemoteChangedDuringTransfer() => new ConflictKind.RemoteChangedDuringTransfer();
}

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ConflictSignal"/> cases.</summary>
public static class ConflictSignalFactory
{
    /// <summary>Creates the <see cref="ConflictSignal.None"/> case.</summary>
    public static ConflictSignal CreateNone() => new ConflictSignal.None();

    /// <summary>Creates the <see cref="ConflictSignal.MoveTargetInUse"/> case.</summary>
    public static ConflictSignal CreateMoveTargetInUse() => new ConflictSignal.MoveTargetInUse();

    /// <summary>Creates the <see cref="ConflictSignal.PreconditionFailed"/> case.</summary>
    public static ConflictSignal CreatePreconditionFailed() => new ConflictSignal.PreconditionFailed();
}

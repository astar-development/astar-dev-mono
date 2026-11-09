namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="PathQuarantineState"/> cases.</summary>
public static class PathQuarantineStateFactory
{
    /// <summary>Creates the clear case.</summary>
    public static PathQuarantineState CreateClear() => new PathQuarantineState.Clear();

    /// <summary>Creates the conflicted case.</summary>
    public static PathQuarantineState CreateConflicted() => new PathQuarantineState.Conflicted();

    /// <summary>Creates the held-by-guard case.</summary>
    public static PathQuarantineState CreateHeldByGuard() => new PathQuarantineState.HeldByGuard();

    /// <summary>Creates the skipped case.</summary>
    public static PathQuarantineState CreateSkipped() => new PathQuarantineState.Skipped();

    /// <summary>Creates the resync-in-progress case.</summary>
    public static PathQuarantineState CreateResyncInProgress() => new PathQuarantineState.ResyncInProgress();
}

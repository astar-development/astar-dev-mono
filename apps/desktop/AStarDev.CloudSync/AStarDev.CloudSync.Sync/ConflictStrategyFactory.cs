namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ConflictStrategy"/> cases.</summary>
public static class ConflictStrategyFactory
{
    /// <summary>Creates the <see cref="ConflictStrategy.LocalWins"/> case.</summary>
    public static ConflictStrategy CreateLocalWins() => new ConflictStrategy.LocalWins();

    /// <summary>Creates the <see cref="ConflictStrategy.RemoteWins"/> case.</summary>
    public static ConflictStrategy CreateRemoteWins() => new ConflictStrategy.RemoteWins();

    /// <summary>Creates the <see cref="ConflictStrategy.KeepBoth"/> case.</summary>
    public static ConflictStrategy CreateKeepBoth() => new ConflictStrategy.KeepBoth();

    /// <summary>Creates the <see cref="ConflictStrategy.Skip"/> case.</summary>
    public static ConflictStrategy CreateSkip() => new ConflictStrategy.Skip();
}

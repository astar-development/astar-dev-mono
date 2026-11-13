namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FileSkipReason"/> cases.</summary>
public static class FileSkipReasonFactory
{
    /// <summary>Creates the <see cref="FileSkipReason.Hidden"/> case.</summary>
    public static FileSkipReason CreateHidden() => new FileSkipReason.Hidden();

    /// <summary>Creates the <see cref="FileSkipReason.Temporary"/> case.</summary>
    public static FileSkipReason CreateTemporary() => new FileSkipReason.Temporary();

    /// <summary>Creates the <see cref="FileSkipReason.OfficeLock"/> case.</summary>
    public static FileSkipReason CreateOfficeLock() => new FileSkipReason.OfficeLock();

    /// <summary>Creates the <see cref="FileSkipReason.SystemFile"/> case.</summary>
    public static FileSkipReason CreateSystemFile() => new FileSkipReason.SystemFile();

    /// <summary>Creates the <see cref="FileSkipReason.SymbolicLink"/> case.</summary>
    public static FileSkipReason CreateSymbolicLink() => new FileSkipReason.SymbolicLink();
}

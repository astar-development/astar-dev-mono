namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ConflictRefusal"/> cases.</summary>
public static class ConflictRefusalFactory
{
    /// <summary>Creates the <see cref="ConflictRefusal.Busy"/> case.</summary>
    public static ConflictRefusal CreateBusy() => new ConflictRefusal.Busy();

    /// <summary>Creates the <see cref="ConflictRefusal.Draining"/> case.</summary>
    public static ConflictRefusal CreateDraining() => new ConflictRefusal.Draining();

    /// <summary>Creates the <see cref="ConflictRefusal.QueueUnreadable"/> case.</summary>
    public static ConflictRefusal CreateQueueUnreadable() => new ConflictRefusal.QueueUnreadable();
}

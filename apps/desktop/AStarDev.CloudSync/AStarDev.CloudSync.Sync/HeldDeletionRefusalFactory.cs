namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="HeldDeletionRefusal"/> cases.</summary>
public static class HeldDeletionRefusalFactory
{
    /// <summary>Creates the <see cref="HeldDeletionRefusal.Busy"/> case.</summary>
    public static HeldDeletionRefusal CreateBusy() => new HeldDeletionRefusal.Busy();

    /// <summary>Creates the <see cref="HeldDeletionRefusal.Draining"/> case.</summary>
    public static HeldDeletionRefusal CreateDraining() => new HeldDeletionRefusal.Draining();

    /// <summary>Creates the <see cref="HeldDeletionRefusal.QueueUnreadable"/> case.</summary>
    public static HeldDeletionRefusal CreateQueueUnreadable() => new HeldDeletionRefusal.QueueUnreadable();

    /// <summary>Creates the <see cref="HeldDeletionRefusal.NoLongerHeld"/> case.</summary>
    public static HeldDeletionRefusal CreateNoLongerHeld() => new HeldDeletionRefusal.NoLongerHeld();

    /// <summary>Creates the <see cref="HeldDeletionRefusal.NotSettled"/> case.</summary>
    public static HeldDeletionRefusal CreateNotSettled() => new HeldDeletionRefusal.NotSettled();
}

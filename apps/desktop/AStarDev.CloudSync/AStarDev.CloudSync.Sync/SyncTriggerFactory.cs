namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncTrigger"/> values.</summary>
public static class SyncTriggerFactory
{
    /// <summary>Creates the <see cref="SyncTrigger.Manual"/> case.</summary>
    public static SyncTrigger CreateManual() => new SyncTrigger.Manual();

    /// <summary>Creates the <see cref="SyncTrigger.Scheduled"/> case.</summary>
    public static SyncTrigger CreateScheduled() => new SyncTrigger.Scheduled();

    /// <summary>Creates the <see cref="SyncTrigger.Tray"/> case.</summary>
    public static SyncTrigger CreateTray() => new SyncTrigger.Tray();

    /// <summary>Creates the <see cref="SyncTrigger.Recovery"/> case.</summary>
    public static SyncTrigger CreateRecovery() => new SyncTrigger.Recovery();
}

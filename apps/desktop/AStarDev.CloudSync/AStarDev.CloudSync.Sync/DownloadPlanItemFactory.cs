namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadPlanItem"/> values.</summary>
public static class DownloadPlanItemFactory
{
    /// <summary>Creates the <see cref="DownloadPlanItem.DownloadFile"/> case.</summary>
    public static DownloadPlanItem CreateDownloadFile(RelativeSyncPath path, RemoteItemSnapshot item) => new DownloadPlanItem.DownloadFile(path, item);

    /// <summary>Creates the <see cref="DownloadPlanItem.CreateLocalFolder"/> case.</summary>
    public static DownloadPlanItem CreateCreateLocalFolder(RelativeSyncPath path, RemoteItemId itemId) => new DownloadPlanItem.CreateLocalFolder(path, itemId);

    /// <summary>Creates the <see cref="DownloadPlanItem.MoveLocal"/> case.</summary>
    public static DownloadPlanItem CreateMoveLocal(RelativeSyncPath from, RelativeSyncPath to, RemoteItemId itemId) => new DownloadPlanItem.MoveLocal(from, to, itemId);

    /// <summary>Creates the <see cref="DownloadPlanItem.DeleteLocal"/> case.</summary>
    public static DownloadPlanItem CreateDeleteLocal(RelativeSyncPath path, RemoteItemId itemId) => new DownloadPlanItem.DeleteLocal(path, itemId);

    /// <summary>Creates the <see cref="DownloadPlanItem.SendToConflicts"/> case.</summary>
    public static DownloadPlanItem CreateSendToConflicts(RelativeSyncPath path, ConflictKind kind) => new DownloadPlanItem.SendToConflicts(path, kind);
}

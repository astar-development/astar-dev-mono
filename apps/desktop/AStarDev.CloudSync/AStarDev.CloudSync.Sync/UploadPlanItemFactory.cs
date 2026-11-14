using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadPlanItem"/> cases.</summary>
public static class UploadPlanItemFactory
{
    /// <summary>Creates the <see cref="UploadPlanItem.EnsureRemoteFolder"/> case.</summary>
    public static UploadPlanItem CreateEnsureRemoteFolder(RelativeSyncPath path) => new UploadPlanItem.EnsureRemoteFolder(path);

    /// <summary>Creates the <see cref="UploadPlanItem.UploadNewFile"/> case, which must fail when the item already exists.</summary>
    public static UploadPlanItem CreateUploadNewFile(RelativeSyncPath path, long size, DateTimeOffset lastWriteUtc) => new UploadPlanItem.UploadNewFile(path, Math.Max(size, 0), lastWriteUtc.ToUniversalTime(), UploadPreconditionFactory.CreateMustNotExist());

    /// <summary>Creates the <see cref="UploadPlanItem.UploadReplace"/> case.</summary>
    public static UploadPlanItem CreateUploadReplace(RelativeSyncPath path, RemoteItemId itemId, long size, DateTimeOffset lastWriteUtc, UploadPrecondition precondition) => new UploadPlanItem.UploadReplace(path, itemId, Math.Max(size, 0), lastWriteUtc.ToUniversalTime(), precondition);

    /// <summary>Creates the <see cref="UploadPlanItem.RemoveRemote"/> case.</summary>
    public static UploadPlanItem CreateRemoveRemote(RelativeSyncPath path, RemoteItemId itemId, RemovePrecondition precondition) => new UploadPlanItem.RemoveRemote(path, itemId, precondition);

    /// <summary>Creates the <see cref="UploadPlanItem.LinkExisting"/> case.</summary>
    public static UploadPlanItem CreateLinkExisting(RelativeSyncPath path, RemoteItemId itemId) => new UploadPlanItem.LinkExisting(path, itemId);

    /// <summary>Creates the <see cref="UploadPlanItem.ForgetBaseline"/> case.</summary>
    public static UploadPlanItem CreateForgetBaseline(RelativeSyncPath path) => new UploadPlanItem.ForgetBaseline(path);

    /// <summary>Creates the <see cref="UploadPlanItem.SendToConflicts"/> case.</summary>
    public static UploadPlanItem CreateSendToConflicts(ConflictFinding finding, Option<RemoteItemSnapshot> remoteItem) => new UploadPlanItem.SendToConflicts(finding, remoteItem);

    /// <summary>Creates the <see cref="UploadPlanItem.Rejected"/> case.</summary>
    public static UploadPlanItem CreateRejected(RelativeSyncPath path, InvalidRemoteName reason) => new UploadPlanItem.Rejected(path, reason);
}

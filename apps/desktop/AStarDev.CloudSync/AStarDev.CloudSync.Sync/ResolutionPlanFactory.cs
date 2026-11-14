namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ResolutionPlan"/> cases.</summary>
public static class ResolutionPlanFactory
{
    /// <summary>Creates the <see cref="ResolutionPlan.LeaveOpen"/> case.</summary>
    public static ResolutionPlan CreateLeaveOpen() => new ResolutionPlan.LeaveOpen();

    /// <summary>Creates the <see cref="ResolutionPlan.UploadReplacingRemote"/> case.</summary>
    public static ResolutionPlan CreateUploadReplacingRemote() => new ResolutionPlan.UploadReplacingRemote();

    /// <summary>Creates the <see cref="ResolutionPlan.UploadAsNewFile"/> case.</summary>
    public static ResolutionPlan CreateUploadAsNewFile() => new ResolutionPlan.UploadAsNewFile();

    /// <summary>Creates the <see cref="ResolutionPlan.DownloadOverLocal"/> case.</summary>
    public static ResolutionPlan CreateDownloadOverLocal() => new ResolutionPlan.DownloadOverLocal();

    /// <summary>Creates the <see cref="ResolutionPlan.TrashLocal"/> case.</summary>
    public static ResolutionPlan CreateTrashLocal() => new ResolutionPlan.TrashLocal();

    /// <summary>Creates the <see cref="ResolutionPlan.DeleteRemote"/> case.</summary>
    public static ResolutionPlan CreateDeleteRemote() => new ResolutionPlan.DeleteRemote();

    /// <summary>Creates the <see cref="ResolutionPlan.KeepBothUploadAndDownload"/> case.</summary>
    public static ResolutionPlan CreateKeepBothUploadAndDownload() => new ResolutionPlan.KeepBothUploadAndDownload();

    /// <summary>Creates the <see cref="ResolutionPlan.KeepLocalMoveRemoteAside"/> case.</summary>
    public static ResolutionPlan CreateKeepLocalMoveRemoteAside() => new ResolutionPlan.KeepLocalMoveRemoteAside();

    /// <summary>Creates the <see cref="ResolutionPlan.ApplyRemoteMoveRenameLocal"/> case.</summary>
    public static ResolutionPlan CreateApplyRemoteMoveRenameLocal() => new ResolutionPlan.ApplyRemoteMoveRenameLocal();

    /// <summary>Creates the <see cref="ResolutionPlan.KeepBothAtMoveTarget"/> case.</summary>
    public static ResolutionPlan CreateKeepBothAtMoveTarget() => new ResolutionPlan.KeepBothAtMoveTarget();
}

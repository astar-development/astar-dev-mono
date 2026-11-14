using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class ConflictResolutionPlanner : IConflictResolutionPlanner
{
    /// <inheritdoc />
    public Result<ResolutionPlan, ResolutionNotAllowed> Plan(SyncAttentionItem item, ConflictStrategy strategy) =>
        item is SyncAttentionItem.Conflict conflict
            ? Checked(conflict, PlanFor(conflict.Kind, strategy))
            : Result.Failure<ResolutionPlan, ResolutionNotAllowed>(ResolutionNotAllowedFactory.CreateNotAConflict());

    private static ResolutionPlan PlanFor(ConflictKind kind, ConflictStrategy strategy) => strategy switch
    {
        ConflictStrategy.LocalWins => LocalWinsFor(kind),
        ConflictStrategy.RemoteWins => RemoteWinsFor(kind),
        ConflictStrategy.KeepBoth => KeepBothFor(kind),
        _ => ResolutionPlanFactory.CreateLeaveOpen(),
    };

    private static ResolutionPlan LocalWinsFor(ConflictKind kind) => kind switch
    {
        ConflictKind.LocalModifiedRemoteDeleted => ResolutionPlanFactory.CreateUploadAsNewFile(),
        ConflictKind.LocalDeletedRemoteModified => ResolutionPlanFactory.CreateDeleteRemote(),
        ConflictKind.MoveTargetTaken => ResolutionPlanFactory.CreateKeepLocalMoveRemoteAside(),
        _ => ResolutionPlanFactory.CreateUploadReplacingRemote(),
    };

    private static ResolutionPlan RemoteWinsFor(ConflictKind kind) => kind switch
    {
        ConflictKind.LocalModifiedRemoteDeleted => ResolutionPlanFactory.CreateTrashLocal(),
        ConflictKind.MoveTargetTaken => ResolutionPlanFactory.CreateApplyRemoteMoveRenameLocal(),
        _ => ResolutionPlanFactory.CreateDownloadOverLocal(),
    };

    private static ResolutionPlan KeepBothFor(ConflictKind kind) => kind switch
    {
        ConflictKind.LocalModifiedRemoteDeleted => ResolutionPlanFactory.CreateUploadAsNewFile(),
        ConflictKind.LocalDeletedRemoteModified => ResolutionPlanFactory.CreateDownloadOverLocal(),
        ConflictKind.MoveTargetTaken => ResolutionPlanFactory.CreateKeepBothAtMoveTarget(),
        _ => ResolutionPlanFactory.CreateKeepBothUploadAndDownload(),
    };

    private static bool NeedsLocal(ResolutionPlan plan) => plan is ResolutionPlan.UploadReplacingRemote or ResolutionPlan.KeepBothUploadAndDownload or ResolutionPlan.UploadAsNewFile or ResolutionPlan.TrashLocal;

    private static bool NeedsRemote(ResolutionPlan plan) => plan is ResolutionPlan.UploadReplacingRemote or ResolutionPlan.KeepBothUploadAndDownload or ResolutionPlan.DownloadOverLocal or ResolutionPlan.DeleteRemote;

    private static Result<ResolutionPlan, ResolutionNotAllowed> Checked(SyncAttentionItem.Conflict conflict, ResolutionPlan plan)
    {
        if (NeedsLocal(plan) && conflict.Local is SideSnapshot.Absent)
            return Result.Failure<ResolutionPlan, ResolutionNotAllowed>(ResolutionNotAllowedFactory.CreateLocalSideAbsent());

        return NeedsRemote(plan) && conflict.Remote is SideSnapshot.Absent
            ? Result.Failure<ResolutionPlan, ResolutionNotAllowed>(ResolutionNotAllowedFactory.CreateRemoteSideAbsent())
            : Result.Success<ResolutionPlan, ResolutionNotAllowed>(plan);
    }
}

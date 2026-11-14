namespace AStarDev.CloudSync.Sync;

/// <summary>What resolving a conflict with a strategy does, decided without touching any file (#1406).</summary>
public abstract record ResolutionPlan
{
    private ResolutionPlan()
    {
    }

    /// <summary>Nothing changes; the conflict stays in the queue.</summary>
    public sealed record LeaveOpen : ResolutionPlan
    {
        internal LeaveOpen()
        {
        }
    }

    /// <summary>Upload the local file as a new version of the cloud file, with an if-match on the cloud file's current content tag.</summary>
    public sealed record UploadReplacingRemote : ResolutionPlan
    {
        internal UploadReplacingRemote()
        {
        }
    }

    /// <summary>Upload the local file as a new cloud file.</summary>
    public sealed record UploadAsNewFile : ResolutionPlan
    {
        internal UploadAsNewFile()
        {
        }
    }

    /// <summary>Download the cloud file over the local file, or to the place where the local file was.</summary>
    public sealed record DownloadOverLocal : ResolutionPlan
    {
        internal DownloadOverLocal()
        {
        }
    }

    /// <summary>Move the local file into the sync trash; nothing is deleted for good.</summary>
    public sealed record TrashLocal : ResolutionPlan
    {
        internal TrashLocal()
        {
        }
    }

    /// <summary>Delete the cloud file, with an if-match on its current content tag.</summary>
    public sealed record DeleteRemote : ResolutionPlan
    {
        internal DeleteRemote()
        {
        }
    }

    /// <summary>Rename the local copy to a Keep Both name, upload it as a new file and download the cloud file to the original name.</summary>
    public sealed record KeepBothUploadAndDownload : ResolutionPlan
    {
        internal KeepBothUploadAndDownload()
        {
        }
    }

    /// <summary>The local item stays and the cloud item is moved to a Keep Both name.</summary>
    public sealed record KeepLocalMoveRemoteAside : ResolutionPlan
    {
        internal KeepLocalMoveRemoteAside()
        {
        }
    }

    /// <summary>The cloud move is applied and the local item is renamed to a Keep Both name.</summary>
    public sealed record ApplyRemoteMoveRenameLocal : ResolutionPlan
    {
        internal ApplyRemoteMoveRenameLocal()
        {
        }
    }

    /// <summary>Both items are kept, each with its own Keep Both name.</summary>
    public sealed record KeepBothAtMoveTarget : ResolutionPlan
    {
        internal KeepBothAtMoveTarget()
        {
        }
    }
}

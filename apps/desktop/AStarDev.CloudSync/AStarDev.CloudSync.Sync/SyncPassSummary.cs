namespace AStarDev.CloudSync.Sync;

/// <summary>What one sync run did for an account (#1402): the totals of each step, and how many conflicts and held deletions wait for the user.</summary>
public sealed record SyncPassSummary
{
    internal SyncPassSummary(RemoteUpdateSummary remote, LocalChangeSet localChanges, DownloadPassSummary download, UploadPassSummary upload)
    {
        Remote = remote;
        LocalChanges = localChanges.Changes.Count;
        HeldLocalScanDeletions = localChanges.Holds.Sum(hold => hold.Missing);
        Download = download;
        Upload = upload;
    }

    /// <summary>What the remote index update found.</summary>
    public RemoteUpdateSummary Remote { get; }

    /// <summary>How many local changes the scan found.</summary>
    public int LocalChanges { get; }

    /// <summary>How many local deletions the scan's mass-deletion guard held.</summary>
    public int HeldLocalScanDeletions { get; }

    /// <summary>What the download pass did.</summary>
    public DownloadPassSummary Download { get; }

    /// <summary>What the upload pass did.</summary>
    public UploadPassSummary Upload { get; }

    /// <summary>Paths both passes sent to conflict handling. They are counted here; writing them to the attention queue is #1534.</summary>
    public int Conflicts => Download.SentToConflicts + Upload.SentToConflicts;

    /// <summary>Deletions held by the mass-deletion guard on either side, waiting for the user to confirm.</summary>
    public int HeldDeletions => Download.HeldByGuard + HeldLocalScanDeletions;
}

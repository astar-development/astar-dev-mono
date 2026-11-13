namespace AStarDev.CloudSync.Sync;

/// <summary>What an upload pass did, as counts. Deletions are counted, so a deletion-only pass never reads as "no changes" (#1155).</summary>
public sealed record UploadPassSummary
{
    internal UploadPassSummary(int uploaded, long bytesUploaded, int foldersCreated, int linked, int removed, int sentToConflicts, int deferred, int failed, IReadOnlyList<UploadItemOutcome.Rejected> rejected)
    {
        Uploaded = uploaded;
        BytesUploaded = bytesUploaded;
        FoldersCreated = foldersCreated;
        Linked = linked;
        Removed = removed;
        SentToConflicts = sentToConflicts;
        Deferred = deferred;
        Failed = failed;
        Rejected = rejected;
    }

    /// <summary>Files uploaded.</summary>
    public int Uploaded { get; }

    /// <summary>Bytes sent for those files.</summary>
    public long BytesUploaded { get; }

    /// <summary>Remote folders created or found.</summary>
    public int FoldersCreated { get; }

    /// <summary>Existing remote folders linked.</summary>
    public int Linked { get; }

    /// <summary>Remote items deleted, or already gone.</summary>
    public int Removed { get; }

    /// <summary>Paths handed to conflict handling.</summary>
    public int SentToConflicts { get; }

    /// <summary>Files that changed while read and wait for the next pass.</summary>
    public int Deferred { get; }

    /// <summary>Steps that failed and are retried on the next pass.</summary>
    public int Failed { get; }

    /// <summary>The items the provider would reject, with the reason, for the user.</summary>
    public IReadOnlyList<UploadItemOutcome.Rejected> Rejected { get; }
}

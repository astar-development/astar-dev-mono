namespace AStarDev.CloudSync.Sync;

/// <summary>What an upload pass did, as counts. Deletions are counted, so a deletion-only pass never reads as "no changes" (#1155).</summary>
public sealed record UploadPassSummary
{
    internal UploadPassSummary(IReadOnlyList<UploadItemOutcome> outcomes)
    {
        Outcomes = outcomes;
    }

    /// <summary>What happened to each planned path.</summary>
    public IReadOnlyList<UploadItemOutcome> Outcomes { get; }

    /// <summary>Files uploaded.</summary>
    public int Uploaded => Outcomes.OfType<UploadItemOutcome.Uploaded>().Count();

    /// <summary>Bytes sent for those files.</summary>
    public long BytesUploaded => Outcomes.OfType<UploadItemOutcome.Uploaded>().Sum(outcome => outcome.Bytes);

    /// <summary>Remote folders created or found.</summary>
    public int FoldersCreated => Outcomes.OfType<UploadItemOutcome.FolderCreated>().Count();

    /// <summary>Existing remote folders linked.</summary>
    public int Linked => Outcomes.OfType<UploadItemOutcome.Linked>().Count();

    /// <summary>Remote items deleted, or already gone.</summary>
    public int Removed => Outcomes.OfType<UploadItemOutcome.RemovedRemotely>().Count();

    /// <summary>Paths handed to conflict handling.</summary>
    public int SentToConflicts => Outcomes.OfType<UploadItemOutcome.SentToConflicts>().Count();

    /// <summary>Files that changed while read and wait for the next pass.</summary>
    public int Deferred => Outcomes.OfType<UploadItemOutcome.Deferred>().Count();

    /// <summary>Steps that failed and are retried on the next pass.</summary>
    public int Failed => Outcomes.OfType<UploadItemOutcome.Failed>().Count();

    /// <summary>The items the provider would reject, with the reason, for the user.</summary>
    public IReadOnlyList<UploadItemOutcome.Rejected> Rejected => [.. Outcomes.OfType<UploadItemOutcome.Rejected>()];
}

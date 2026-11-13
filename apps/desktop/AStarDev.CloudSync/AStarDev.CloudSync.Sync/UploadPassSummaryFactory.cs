namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadPassSummary"/> values.</summary>
public static class UploadPassSummaryFactory
{
    /// <summary>Counts the outcomes of a pass.</summary>
    public static UploadPassSummary Create(IEnumerable<UploadItemOutcome> outcomes)
    {
        var all = outcomes.ToList();
        var uploaded = all.OfType<UploadItemOutcome.Uploaded>().ToList();

        return new UploadPassSummary(
            uploaded.Count,
            uploaded.Sum(outcome => outcome.Bytes),
            all.OfType<UploadItemOutcome.FolderCreated>().Count(),
            all.OfType<UploadItemOutcome.Linked>().Count(),
            all.OfType<UploadItemOutcome.RemovedRemotely>().Count(),
            all.OfType<UploadItemOutcome.SentToConflicts>().Count(),
            all.OfType<UploadItemOutcome.Deferred>().Count(),
            all.OfType<UploadItemOutcome.Failed>().Count(),
            [.. all.OfType<UploadItemOutcome.Rejected>()]);
    }
}

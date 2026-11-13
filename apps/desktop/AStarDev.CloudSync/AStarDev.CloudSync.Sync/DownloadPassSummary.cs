namespace AStarDev.CloudSync.Sync;

/// <summary>What one download pass did (#1400): a count per outcome, deletions and moves included, and the pairs the guard held.</summary>
public sealed record DownloadPassSummary
{
    internal DownloadPassSummary(IReadOnlyList<DownloadItemOutcome> outcomes, IReadOnlyList<HeldLocalDeletions> held)
    {
        Outcomes = outcomes;
        Held = held;
    }

    /// <summary>One outcome per executed item.</summary>
    public IReadOnlyList<DownloadItemOutcome> Outcomes { get; }

    /// <summary>The pairs whose deletions the guard held, for the user to confirm.</summary>
    public IReadOnlyList<HeldLocalDeletions> Held { get; }

    /// <summary>Files downloaded.</summary>
    public int Downloaded => Outcomes.OfType<DownloadItemOutcome.Downloaded>().Count();

    /// <summary>Bytes downloaded.</summary>
    public long Bytes => Outcomes.OfType<DownloadItemOutcome.Downloaded>().Sum(outcome => outcome.Bytes);

    /// <summary>Local folders created.</summary>
    public int FoldersCreated => Outcomes.OfType<DownloadItemOutcome.FolderCreated>().Count();

    /// <summary>Local items moved.</summary>
    public int Moved => Outcomes.OfType<DownloadItemOutcome.Moved>().Count();

    /// <summary>Local items deleted.</summary>
    public int DeletedLocally => Outcomes.OfType<DownloadItemOutcome.DeletedLocally>().Count();

    /// <summary>Deletions the guard held, counted by item.</summary>
    public int HeldByGuard => Math.Max(Outcomes.OfType<DownloadItemOutcome.HeldByGuard>().Count(), Held.Sum(pair => pair.Paths.Count));

    /// <summary>Paths sent to the attention queue.</summary>
    public int SentToConflicts => Outcomes.OfType<DownloadItemOutcome.SentToConflicts>().Count();

    /// <summary>Items left alone.</summary>
    public int Skipped => Outcomes.OfType<DownloadItemOutcome.Skipped>().Count();

    /// <summary>Items that failed.</summary>
    public int Failed => Outcomes.OfType<DownloadItemOutcome.Failed>().Count();
}

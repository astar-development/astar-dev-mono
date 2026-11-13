namespace AStarDev.CloudSync.Sync;

/// <summary>What one download pass did (#1400): a count per outcome, deletions and moves included, and the pairs the guard held.</summary>
public sealed record DownloadPassSummary
{
    internal DownloadPassSummary(int downloaded, long bytes, int foldersCreated, int moved, int deletedLocally, int heldByGuard, int sentToConflicts, int skipped, int failed, IReadOnlyList<HeldLocalDeletions> held)
    {
        Downloaded = downloaded;
        Bytes = bytes;
        FoldersCreated = foldersCreated;
        Moved = moved;
        DeletedLocally = deletedLocally;
        HeldByGuard = heldByGuard;
        SentToConflicts = sentToConflicts;
        Skipped = skipped;
        Failed = failed;
        Held = held;
    }

    /// <summary>Files downloaded.</summary>
    public int Downloaded { get; }

    /// <summary>Bytes downloaded.</summary>
    public long Bytes { get; }

    /// <summary>Local folders created.</summary>
    public int FoldersCreated { get; }

    /// <summary>Local items moved.</summary>
    public int Moved { get; }

    /// <summary>Local items deleted.</summary>
    public int DeletedLocally { get; }

    /// <summary>Deletions the guard held, counted by item.</summary>
    public int HeldByGuard { get; }

    /// <summary>Paths sent to the attention queue.</summary>
    public int SentToConflicts { get; }

    /// <summary>Items left alone.</summary>
    public int Skipped { get; }

    /// <summary>Items that failed.</summary>
    public int Failed { get; }

    /// <summary>The pairs whose deletions the guard held, for the user to confirm.</summary>
    public IReadOnlyList<HeldLocalDeletions> Held { get; }
}

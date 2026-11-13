namespace AStarDev.CloudSync.Sync;

/// <summary>What a local scan found, counted per kind of change (#1399). A deletion-only result is never "no changes".</summary>
public sealed record LocalScanSummary
{
    internal LocalScanSummary(int added, int modified, int deleted, int deferred, int unreadable, int touched, int hashed)
    {
        Added = added;
        Modified = modified;
        Deleted = deleted;
        Deferred = deferred;
        Unreadable = unreadable;
        Touched = touched;
        Hashed = hashed;
    }

    /// <summary>New items.</summary>
    public int Added { get; }

    /// <summary>Files whose content changed.</summary>
    public int Modified { get; }

    /// <summary>Items that are gone from disk.</summary>
    public int Deleted { get; }

    /// <summary>Files left for the next pass because they changed in the last two seconds.</summary>
    public int Deferred { get; }

    /// <summary>Items the scan could not read.</summary>
    public int Unreadable { get; }

    /// <summary>Files that were only touched: the write time changed but the content did not, so the baseline time was refreshed and no change is reported.</summary>
    public int Touched { get; }

    /// <summary>Files that were hashed.</summary>
    public int Hashed { get; }
}

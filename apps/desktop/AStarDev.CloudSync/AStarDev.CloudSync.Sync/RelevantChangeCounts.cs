namespace AStarDev.CloudSync.Sync;

/// <summary>How many remote changes matter to an account's selection (#1398): adds, content changes, moves and deletes.</summary>
public sealed record RelevantChangeCounts
{
    internal RelevantChangeCounts(int added, int modified, int moved, int deleted)
    {
        Added = added;
        Modified = modified;
        Moved = moved;
        Deleted = deleted;
    }

    /// <summary>Items that are new, or moved into the selection.</summary>
    public int Added { get; }

    /// <summary>Items whose content changed.</summary>
    public int Modified { get; }

    /// <summary>Items moved or renamed inside the selection.</summary>
    public int Moved { get; }

    /// <summary>Items deleted, or moved out of the selection.</summary>
    public int Deleted { get; }
}

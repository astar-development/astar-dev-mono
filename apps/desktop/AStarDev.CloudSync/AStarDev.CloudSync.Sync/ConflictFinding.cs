namespace AStarDev.CloudSync.Sync;

/// <summary>A path a pass found in conflict: why, and what both sides held at that moment (#1534). The sync pass runner turns it into an attention item.</summary>
public sealed record ConflictFinding
{
    internal ConflictFinding(RelativeSyncPath path, ConflictKind kind, SideSnapshot local, SideSnapshot remote)
    {
        Path = path;
        Kind = kind;
        Local = local;
        Remote = remote;
    }

    /// <summary>The conflicted path.</summary>
    public RelativeSyncPath Path { get; }

    /// <summary>Why it conflicts.</summary>
    public ConflictKind Kind { get; }

    /// <summary>What the local side held.</summary>
    public SideSnapshot Local { get; }

    /// <summary>What the cloud held.</summary>
    public SideSnapshot Remote { get; }
}

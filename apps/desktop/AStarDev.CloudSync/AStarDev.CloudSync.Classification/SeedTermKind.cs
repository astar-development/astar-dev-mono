namespace AStarDev.CloudSync.Classification;

/// <summary>What a seed term matches: a word in the path, a file extension or a folder name.</summary>
public abstract record SeedTermKind
{
    private SeedTermKind()
    {
    }

    /// <summary>A word anywhere in the path.</summary>
    public sealed record Word : SeedTermKind
    {
        internal Word()
        {
        }
    }

    /// <summary>A file extension without the dot.</summary>
    public sealed record Extension : SeedTermKind
    {
        internal Extension()
        {
        }
    }

    /// <summary>A folder name.</summary>
    public sealed record Folder : SeedTermKind
    {
        internal Folder()
        {
        }
    }
}

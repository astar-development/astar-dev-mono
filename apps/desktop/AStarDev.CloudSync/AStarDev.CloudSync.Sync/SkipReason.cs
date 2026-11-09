namespace AStarDev.CloudSync.Sync;

/// <summary>Why a local folder is left out of the tree and the scan (#1395).</summary>
public abstract record SkipReason
{
    private SkipReason()
    {
    }

    /// <summary>The folder has the hidden attribute.</summary>
    public sealed record Hidden : SkipReason
    {
        internal Hidden()
        {
        }
    }

    /// <summary>The folder name starts with a dot.</summary>
    public sealed record DotPrefixed : SkipReason
    {
        internal DotPrefixed()
        {
        }
    }

    /// <summary>The folder is a symbolic link or junction, which is not followed.</summary>
    public sealed record SymbolicLink : SkipReason
    {
        internal SymbolicLink()
        {
        }
    }

    /// <summary>The folder is one of the app's own temporary download items.</summary>
    public sealed record AppTemporary : SkipReason
    {
        internal AppTemporary()
        {
        }
    }
}

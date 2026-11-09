namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a local folder shows in the tree and is scanned.</summary>
public abstract record LocalFolderVisibility
{
    private LocalFolderVisibility()
    {
    }

    /// <summary>The folder shows.</summary>
    public sealed record Visible : LocalFolderVisibility
    {
        internal Visible()
        {
        }
    }

    /// <summary>The folder is left out.</summary>
    public sealed record Skipped : LocalFolderVisibility
    {
        internal Skipped(SkipReason reason)
        {
            Reason = reason;
        }

        /// <summary>The Reason.</summary>
        public SkipReason Reason { get; }
    }
}

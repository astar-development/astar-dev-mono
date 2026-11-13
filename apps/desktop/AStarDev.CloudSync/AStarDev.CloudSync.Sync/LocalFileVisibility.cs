namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a local file is scanned.</summary>
public abstract record LocalFileVisibility
{
    private LocalFileVisibility()
    {
    }

    /// <summary>The file is scanned.</summary>
    public sealed record Visible : LocalFileVisibility
    {
        internal Visible()
        {
        }
    }

    /// <summary>The file is left out.</summary>
    public sealed record Skipped : LocalFileVisibility
    {
        internal Skipped(FileSkipReason reason)
        {
            Reason = reason;
        }

        /// <summary>Why it is left out.</summary>
        public FileSkipReason Reason { get; }
    }
}

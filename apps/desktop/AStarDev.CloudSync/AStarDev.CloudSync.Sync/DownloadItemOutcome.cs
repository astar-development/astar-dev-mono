namespace AStarDev.CloudSync.Sync;

/// <summary>What happened to one planned item of the download pass (#1400).</summary>
public abstract record DownloadItemOutcome
{
    private DownloadItemOutcome()
    {
    }

    /// <summary>A file was downloaded.</summary>
    public sealed record Downloaded : DownloadItemOutcome
    {
        internal Downloaded(long bytes) => Bytes = bytes;

        /// <summary>The bytes written.</summary>
        public long Bytes { get; }
    }

    /// <summary>A local folder was created or linked.</summary>
    public sealed record FolderCreated : DownloadItemOutcome
    {
        internal FolderCreated()
        {
        }
    }

    /// <summary>A local item was moved.</summary>
    public sealed record Moved : DownloadItemOutcome
    {
        internal Moved()
        {
        }
    }

    /// <summary>A local item was deleted.</summary>
    public sealed record DeletedLocally : DownloadItemOutcome
    {
        internal DeletedLocally()
        {
        }
    }

    /// <summary>The mass-deletion guard held the deletion.</summary>
    public sealed record HeldByGuard : DownloadItemOutcome
    {
        internal HeldByGuard()
        {
        }
    }

    /// <summary>The path went to the attention queue.</summary>
    public sealed record SentToConflicts : DownloadItemOutcome
    {
        internal SentToConflicts(ConflictFinding finding) => Finding = finding;

        /// <summary>The conflict with both sides' snapshots.</summary>
        public ConflictFinding Finding { get; }

        /// <summary>Why the path conflicts.</summary>
        public ConflictKind Kind => Finding.Kind;
    }

    /// <summary>A file both sides added with the same size and content hash was linked in the baseline instead of becoming a conflict (#1534).</summary>
    public sealed record LinkedExisting : DownloadItemOutcome
    {
        internal LinkedExisting()
        {
        }
    }

    /// <summary>The item was left alone.</summary>
    public sealed record Skipped : DownloadItemOutcome
    {
        internal Skipped(DownloadSkipReason reason) => Reason = reason;

        /// <summary>Why.</summary>
        public DownloadSkipReason Reason { get; }
    }

    /// <summary>The item failed; a later pass tries again.</summary>
    public sealed record Failed : DownloadItemOutcome
    {
        internal Failed(DownloadFailure failure) => Failure = failure;

        /// <summary>Why it failed.</summary>
        public DownloadFailure Failure { get; }
    }
}

namespace AStarDev.CloudSync.Sync;

/// <summary>What happened to one planned path in an upload pass.</summary>
public abstract record UploadItemOutcome
{
    private UploadItemOutcome()
    {
    }

    /// <summary>The path the outcome is for.</summary>
    public abstract RelativeSyncPath Path { get; }

    /// <summary>The file was uploaded.</summary>
    public sealed record Uploaded : UploadItemOutcome
    {
        internal Uploaded(RelativeSyncPath path, long bytes)
        {
            Path = path;
            Bytes = bytes;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The bytes sent.</summary>
        public long Bytes { get; }
    }

    /// <summary>The remote folder was created or found.</summary>
    public sealed record FolderCreated : UploadItemOutcome
    {
        internal FolderCreated(RelativeSyncPath path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }
    }

    /// <summary>The remote folder already existed and was linked.</summary>
    public sealed record Linked : UploadItemOutcome
    {
        internal Linked(RelativeSyncPath path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }
    }

    /// <summary>The remote item was deleted, or was already gone.</summary>
    public sealed record RemovedRemotely : UploadItemOutcome
    {
        internal RemovedRemotely(RelativeSyncPath path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }
    }

    /// <summary>Both sides changed, so the path went to conflict handling and nothing was touched.</summary>
    public sealed record SentToConflicts : UploadItemOutcome
    {
        internal SentToConflicts(RelativeSyncPath path, ConflictKind kind)
        {
            Path = path;
            Kind = kind;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>What kind of conflict it is.</summary>
        public ConflictKind Kind { get; }
    }

    /// <summary>The file changed while it was read, so it waits for the next pass.</summary>
    public sealed record Deferred : UploadItemOutcome
    {
        internal Deferred(RelativeSyncPath path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }
    }

    /// <summary>The provider would reject the name, path or size.</summary>
    public sealed record Rejected : UploadItemOutcome
    {
        internal Rejected(RelativeSyncPath path, InvalidRemoteName reason)
        {
            Path = path;
            Reason = reason;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>Why it was rejected.</summary>
        public InvalidRemoteName Reason { get; }
    }

    /// <summary>The step failed and is retried on the next pass.</summary>
    public sealed record Failed : UploadItemOutcome
    {
        internal Failed(RelativeSyncPath path, ProviderError error)
        {
            Path = path;
            Error = error;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>What the provider reported.</summary>
        public ProviderError Error { get; }
    }
}

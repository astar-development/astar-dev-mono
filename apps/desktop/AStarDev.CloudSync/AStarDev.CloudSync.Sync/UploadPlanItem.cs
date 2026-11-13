namespace AStarDev.CloudSync.Sync;

/// <summary>One step of an upload pass for one path.</summary>
public abstract record UploadPlanItem
{
    private UploadPlanItem()
    {
    }

    /// <summary>The path the step is for.</summary>
    public abstract RelativeSyncPath Path { get; }

    /// <summary>Create the folder on the provider, or find it when it already exists.</summary>
    public sealed record EnsureRemoteFolder : UploadPlanItem
    {
        internal EnsureRemoteFolder(RelativeSyncPath path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }
    }

    /// <summary>Upload a file that does not exist remotely yet.</summary>
    public sealed record UploadNewFile : UploadPlanItem
    {
        internal UploadNewFile(RelativeSyncPath path, long size, DateTimeOffset lastWriteUtc, UploadPrecondition precondition)
        {
            Path = path;
            Size = size;
            LastWriteUtc = lastWriteUtc;
            Precondition = precondition;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The local size in bytes.</summary>
        public long Size { get; }

        /// <summary>The local write time, sent to the provider as the modified time.</summary>
        public DateTimeOffset LastWriteUtc { get; }

        /// <summary>The upload must fail when the item already exists.</summary>
        public UploadPrecondition Precondition { get; }
    }

    /// <summary>Upload a new version of a file whose remote copy has not changed since the baseline.</summary>
    public sealed record UploadReplace : UploadPlanItem
    {
        internal UploadReplace(RelativeSyncPath path, RemoteItemId itemId, long size, DateTimeOffset lastWriteUtc, UploadPrecondition precondition)
        {
            Path = path;
            ItemId = itemId;
            Size = size;
            LastWriteUtc = lastWriteUtc;
            Precondition = precondition;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The remote file to replace.</summary>
        public RemoteItemId ItemId { get; }

        /// <summary>The local size in bytes.</summary>
        public long Size { get; }

        /// <summary>The local write time, sent to the provider as the modified time.</summary>
        public DateTimeOffset LastWriteUtc { get; }

        /// <summary>The upload must fail when the remote file no longer has the baseline tag.</summary>
        public UploadPrecondition Precondition { get; }
    }

    /// <summary>Delete the remote item because it was deleted locally and has not changed remotely. A file carries its content tag, a folder the entity tag.</summary>
    public sealed record RemoveRemote : UploadPlanItem
    {
        internal RemoveRemote(RelativeSyncPath path, RemoteItemId itemId, RemovePrecondition precondition)
        {
            Path = path;
            ItemId = itemId;
            Precondition = precondition;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The remote item to delete.</summary>
        public RemoteItemId ItemId { get; }

        /// <summary>The delete must fail when the remote item no longer has the tag last seen.</summary>
        public RemovePrecondition Precondition { get; }
    }

    /// <summary>A new local folder already exists remotely: record it in the baseline without uploading.</summary>
    public sealed record LinkExisting : UploadPlanItem
    {
        internal LinkExisting(RelativeSyncPath path, RemoteItemId itemId)
        {
            Path = path;
            ItemId = itemId;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The remote folder.</summary>
        public RemoteItemId ItemId { get; }
    }

    /// <summary>The remote item is already gone: drop the baseline row only.</summary>
    public sealed record ForgetBaseline : UploadPlanItem
    {
        internal ForgetBaseline(RelativeSyncPath path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }
    }

    /// <summary>Both sides changed: hand the path to conflict handling and touch nothing.</summary>
    public sealed record SendToConflicts : UploadPlanItem
    {
        internal SendToConflicts(RelativeSyncPath path, ConflictKind kind)
        {
            Path = path;
            Kind = kind;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>What kind of conflict it is.</summary>
        public ConflictKind Kind { get; }
    }

    /// <summary>The provider would reject the item: skip it and tell the user.</summary>
    public sealed record Rejected : UploadPlanItem
    {
        internal Rejected(RelativeSyncPath path, InvalidRemoteName reason)
        {
            Path = path;
            Reason = reason;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>Why the provider would reject it.</summary>
        public InvalidRemoteName Reason { get; }
    }
}

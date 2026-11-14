using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>One step the download pass takes for one path (#1400).</summary>
public abstract record DownloadPlanItem
{
    private DownloadPlanItem()
    {
    }

    /// <summary>The path the step is keyed by: the target path for a move.</summary>
    public abstract RelativeSyncPath Path { get; }

    /// <summary>Download a file, overwriting the local copy when there is one.</summary>
    public sealed record DownloadFile : DownloadPlanItem
    {
        internal DownloadFile(RelativeSyncPath path, RemoteItemSnapshot item)
        {
            Path = path;
            Item = item;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The remote file to download.</summary>
        public RemoteItemSnapshot Item { get; }
    }

    /// <summary>Create a local folder; an existing folder is left as it is and gets its baseline row.</summary>
    public sealed record CreateLocalFolder : DownloadPlanItem
    {
        internal CreateLocalFolder(RelativeSyncPath path, RemoteItemId itemId)
        {
            Path = path;
            ItemId = itemId;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The remote folder.</summary>
        public RemoteItemId ItemId { get; }
    }

    /// <summary>Move or rename a local file or folder, without downloading it again.</summary>
    public sealed record MoveLocal : DownloadPlanItem
    {
        internal MoveLocal(RelativeSyncPath from, RelativeSyncPath to, RemoteItemId itemId)
        {
            From = from;
            To = to;
            ItemId = itemId;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path => To;

        /// <summary>Where the item is now.</summary>
        public RelativeSyncPath From { get; }

        /// <summary>Where the item goes.</summary>
        public RelativeSyncPath To { get; }

        /// <summary>The remote item.</summary>
        public RemoteItemId ItemId { get; }
    }

    /// <summary>Delete a local file or folder because the remote item is gone or left the selection.</summary>
    public sealed record DeleteLocal : DownloadPlanItem
    {
        internal DeleteLocal(RelativeSyncPath path, RemoteItemId itemId)
        {
            Path = path;
            ItemId = itemId;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The remote item the baseline linked to the path.</summary>
        public RemoteItemId ItemId { get; }
    }

    /// <summary>Both sides changed, so nothing is touched and the path goes to the attention queue (#1405).</summary>
    public sealed record SendToConflicts : DownloadPlanItem
    {
        internal SendToConflicts(ConflictFinding finding, Option<RemoteItemSnapshot> remoteItem)
        {
            Finding = finding;
            RemoteItem = remoteItem;
        }

        /// <summary>The conflict with both sides' snapshots.</summary>
        public ConflictFinding Finding { get; }

        /// <summary>The cloud item the conflict is about, when there is one; a first-sync link needs its id and tags.</summary>
        public Option<RemoteItemSnapshot> RemoteItem { get; }

        /// <inheritdoc />
        public override RelativeSyncPath Path => Finding.Path;

        /// <summary>Why the path conflicts.</summary>
        public ConflictKind Kind => Finding.Kind;
    }
}

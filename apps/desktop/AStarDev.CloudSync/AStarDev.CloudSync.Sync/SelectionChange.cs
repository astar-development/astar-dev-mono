using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>A staged change to the selection rules, applied together when the user presses Apply (#1397).</summary>
public abstract record SelectionChange
{
    private SelectionChange()
    {
    }

    /// <summary>Include a folder and drop every rule below it.</summary>
    public sealed record IncludeFolder : SelectionChange
    {
        internal IncludeFolder(LocalRelativePath path, Option<RemoteItemId> remoteId, SyncDirection direction)
        {
            Path = path;
            RemoteId = remoteId;
            Direction = direction;
        }

        /// <summary>The Path.</summary>
        public LocalRelativePath Path { get; }

        /// <summary>The RemoteId.</summary>
        public Option<RemoteItemId> RemoteId { get; }

        /// <summary>The Direction.</summary>
        public SyncDirection Direction { get; }
    }

    /// <summary>Exclude a folder and drop every rule below it.</summary>
    public sealed record ExcludeFolder : SelectionChange
    {
        internal ExcludeFolder(LocalRelativePath path, Option<RemoteItemId> remoteId)
        {
            Path = path;
            RemoteId = remoteId;
        }

        /// <summary>The Path.</summary>
        public LocalRelativePath Path { get; }

        /// <summary>The RemoteId.</summary>
        public Option<RemoteItemId> RemoteId { get; }
    }

    /// <summary>Change the direction of a whole sync pair.</summary>
    public sealed record ChangeDirection : SelectionChange
    {
        internal ChangeDirection(SyncPairId pair, SyncDirection direction)
        {
            Pair = pair;
            Direction = direction;
        }

        /// <summary>The Pair.</summary>
        public SyncPairId Pair { get; }

        /// <summary>The Direction.</summary>
        public SyncDirection Direction { get; }
    }
}

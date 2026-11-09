using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>What applying a page changed in the remote index (#1398).</summary>
public abstract record RemoteChange
{
    private RemoteChange()
    {
    }

    /// <summary>A new item.</summary>
    public sealed record Added : RemoteChange
    {
        internal Added(RemoteItemSnapshot item)
        {
            Item = item;
        }

        /// <summary>The item as reported.</summary>
        public RemoteItemSnapshot Item { get; }

        /// <summary>Returns a redacted form.</summary>
        public override string ToString() => "Added(redacted)";
    }

    /// <summary>The content changed (cTag or hash differs); eTag-only changes are ignored.</summary>
    public sealed record Modified : RemoteChange
    {
        internal Modified(RemoteItemSnapshot item)
        {
            Item = item;
        }

        /// <summary>The item as reported.</summary>
        public RemoteItemSnapshot Item { get; }

        /// <summary>Returns a redacted form.</summary>
        public override string ToString() => "Modified(redacted)";
    }

    /// <summary>The parent or the name changed.</summary>
    public sealed record Moved : RemoteChange
    {
        internal Moved(RemoteItemSnapshot item, Option<RemoteItemId> oldParentId, string oldName)
        {
            Item = item;
            OldParentId = oldParentId;
            OldName = oldName;
        }

        /// <summary>The item as reported, with its new parent and name.</summary>
        public RemoteItemSnapshot Item { get; }

        /// <summary>The parent before the move; none for the root.</summary>
        public Option<RemoteItemId> OldParentId { get; }

        /// <summary>The name before the move.</summary>
        public string OldName { get; }

        /// <summary>Returns a redacted form.</summary>
        public override string ToString() => "Moved(redacted)";
    }

    /// <summary>The item was deleted.</summary>
    public sealed record Deleted : RemoteChange
    {
        internal Deleted(RemoteItemId id)
        {
            Id = id;
        }

        /// <summary>The deleted item.</summary>
        public RemoteItemId Id { get; }

        /// <summary>Returns a redacted form.</summary>
        public override string ToString() => "Deleted(redacted)";
    }
}

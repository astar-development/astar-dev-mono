using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Which folder of the selection tree to list the children of (#1397).</summary>
public abstract record FolderTreePosition
{
    private FolderTreePosition()
    {
    }

    /// <summary>The sync root and the drive root.</summary>
    public sealed record Root : FolderTreePosition
    {
        internal Root()
        {
        }
    }

    /// <summary>A folder. It exists locally under its path; it exists in the cloud when it has a remote id.</summary>
    public sealed record Folder : FolderTreePosition
    {
        internal Folder(LocalRelativePath path, Option<RemoteItemId> remoteId)
        {
            Path = path;
            RemoteId = remoteId;
        }

        /// <summary>The Path.</summary>
        public LocalRelativePath Path { get; }

        /// <summary>The RemoteId.</summary>
        public Option<RemoteItemId> RemoteId { get; }
    }
}

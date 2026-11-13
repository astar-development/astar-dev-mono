using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Where an uploaded file goes.</summary>
public abstract record UploadTarget
{
    private UploadTarget()
    {
    }

    /// <summary>A new file in a folder; its name is the local file's name.</summary>
    public sealed record NewFileIn : UploadTarget
    {
        internal NewFileIn(Option<RemoteItemId> parent)
        {
            Parent = parent;
        }

        /// <summary>The remote folder, or none for the root of the drive.</summary>
        public Option<RemoteItemId> Parent { get; }
    }

    /// <summary>A new version of a file that already exists.</summary>
    public sealed record ReplacingItem : UploadTarget
    {
        internal ReplacingItem(RemoteItemId itemId)
        {
            ItemId = itemId;
        }

        /// <summary>The remote file to replace.</summary>
        public RemoteItemId ItemId { get; }
    }
}

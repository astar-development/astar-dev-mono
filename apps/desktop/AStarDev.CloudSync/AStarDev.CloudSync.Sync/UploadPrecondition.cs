namespace AStarDev.CloudSync.Sync;

/// <summary>What must be true on the provider for an upload to go ahead.</summary>
public abstract record UploadPrecondition
{
    private UploadPrecondition()
    {
    }

    /// <summary>No item with the name may exist (a new upload).</summary>
    public sealed record MustNotExist : UploadPrecondition
    {
        internal MustNotExist()
        {
        }
    }

    /// <summary>The remote file must still have this content tag (a replace).</summary>
    public sealed record MustMatch : UploadPrecondition
    {
        internal MustMatch(ContentTag cTag)
        {
            CTag = cTag;
        }

        /// <summary>The content tag last seen.</summary>
        public ContentTag CTag { get; }
    }
}

namespace AStarDev.CloudSync.Sync;

/// <summary>What must be true on the provider for a remove to go ahead.</summary>
public abstract record RemovePrecondition
{
    private RemovePrecondition()
    {
    }

    /// <summary>The remote item must still have this entity tag.</summary>
    public sealed record MustMatch : RemovePrecondition
    {
        internal MustMatch(EntityTag eTag)
        {
            ETag = eTag;
        }

        /// <summary>The entity tag last seen.</summary>
        public EntityTag ETag { get; }
    }

    /// <summary>The remote file must still have this content tag. A file's content tag changes only when its content does, so renames and sharing changes do not make the delete fail.</summary>
    public sealed record MustMatchContent : RemovePrecondition
    {
        internal MustMatchContent(ContentTag cTag)
        {
            CTag = cTag;
        }

        /// <summary>The content tag last seen.</summary>
        public ContentTag CTag { get; }
    }
}

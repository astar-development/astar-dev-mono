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
        internal MustMatch(string eTag)
        {
            ETag = eTag;
        }

        /// <summary>The entity tag last seen.</summary>
        public string ETag { get; }
    }
}

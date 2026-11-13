namespace AStarDev.CloudSync.Sync;

/// <summary>Which side the held deletions would be applied to.</summary>
public abstract record DeletionDirection
{
    private DeletionDirection()
    {
    }

    /// <summary>Items deleted in the cloud would be deleted locally.</summary>
    public sealed record DeleteLocal : DeletionDirection
    {
        internal DeleteLocal()
        {
        }
    }

    /// <summary>Items deleted locally would be deleted in the cloud.</summary>
    public sealed record DeleteRemote : DeletionDirection
    {
        internal DeleteRemote()
        {
        }
    }
}

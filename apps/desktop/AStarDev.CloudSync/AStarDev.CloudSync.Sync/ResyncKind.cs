namespace AStarDev.CloudSync.Sync;

/// <summary>What a full resync must do after the change cursor expired (Graph 410).</summary>
public abstract record ResyncKind
{
    private ResyncKind()
    {
    }

    /// <summary>Apply remote differences to the local copy.</summary>
    public sealed record ApplyDifferences : ResyncKind
    {
        internal ApplyDifferences()
        {
        }
    }

    /// <summary>Upload local differences, the safer choice.</summary>
    public sealed record UploadDifferences : ResyncKind
    {
        internal UploadDifferences()
        {
        }
    }

    /// <summary>The provider did not say; handled like <see cref="UploadDifferences"/>.</summary>
    public sealed record Unknown : ResyncKind
    {
        internal Unknown()
        {
        }
    }
}

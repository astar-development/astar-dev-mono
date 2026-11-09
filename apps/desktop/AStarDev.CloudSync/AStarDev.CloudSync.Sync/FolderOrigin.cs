namespace AStarDev.CloudSync.Sync;

/// <summary>Where a folder in the selection tree exists (#1397).</summary>
public abstract record FolderOrigin
{
    private FolderOrigin()
    {
    }

    /// <summary>The folder exists locally and in the cloud.</summary>
    public sealed record Both : FolderOrigin
    {
        internal Both()
        {
        }
    }

    /// <summary>The folder exists only in the cloud.</summary>
    public sealed record RemoteOnly : FolderOrigin
    {
        internal RemoteOnly()
        {
        }
    }

    /// <summary>The folder exists only on disk.</summary>
    public sealed record LocalOnly : FolderOrigin
    {
        internal LocalOnly()
        {
        }
    }
}

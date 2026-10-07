namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>Where a folder in the merged tree was found.</summary>
public abstract record FolderOrigin
{
    /// <summary>The folder exists on OneDrive but not in the local sync folder.</summary>
    public sealed record RemoteOnly : FolderOrigin;

    /// <summary>The folder exists in the local sync folder but not on OneDrive.</summary>
    public sealed record LocalOnly : FolderOrigin;

    /// <summary>The folder exists both on OneDrive and in the local sync folder.</summary>
    public sealed record Both : FolderOrigin;
}

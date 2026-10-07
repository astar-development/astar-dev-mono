namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>Creates <see cref="FolderOrigin"/> instances for each origin case.</summary>
public static class FolderOriginFactory
{
    /// <summary>Creates the origin for a folder that only exists on OneDrive.</summary>
    public static FolderOrigin CreateRemoteOnly() => new FolderOrigin.RemoteOnly();

    /// <summary>Creates the origin for a folder that only exists locally.</summary>
    public static FolderOrigin CreateLocalOnly() => new FolderOrigin.LocalOnly();

    /// <summary>Creates the origin for a folder that exists on both sides.</summary>
    public static FolderOrigin CreateBoth() => new FolderOrigin.Both();
}

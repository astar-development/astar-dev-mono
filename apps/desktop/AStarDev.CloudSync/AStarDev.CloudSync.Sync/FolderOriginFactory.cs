namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FolderOrigin"/> cases.</summary>
public static class FolderOriginFactory
{
    /// <summary>Creates the <see cref="FolderOrigin.Both"/> case.</summary>
    public static FolderOrigin CreateBoth() => new FolderOrigin.Both();

    /// <summary>Creates the <see cref="FolderOrigin.RemoteOnly"/> case.</summary>
    public static FolderOrigin CreateRemoteOnly() => new FolderOrigin.RemoteOnly();

    /// <summary>Creates the <see cref="FolderOrigin.LocalOnly"/> case.</summary>
    public static FolderOrigin CreateLocalOnly() => new FolderOrigin.LocalOnly();
}

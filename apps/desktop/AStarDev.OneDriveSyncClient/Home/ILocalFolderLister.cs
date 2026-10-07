namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>Lists the child folders of a local sync folder so they can be merged into the remote folder tree.</summary>
public interface ILocalFolderLister
{
    /// <summary>Returns the names of the visible child folders of the local folder that corresponds to <paramref name="remotePath"/>, or an empty list when the folder does not exist.</summary>
    IReadOnlyList<string> ListChildFolderNames(string localSyncRoot, string remotePath);
}

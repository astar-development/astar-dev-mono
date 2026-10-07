using System.IO.Abstractions;
using AStarDev.Utilities;

namespace AStarDev.OneDriveSyncClient.Home;

/// <inheritdoc />
public sealed class LocalFolderLister(IFileSystem fileSystem) : ILocalFolderLister
{
    /// <inheritdoc />
    public IReadOnlyList<string> ListChildFolderNames(string localSyncRoot, string remotePath)
    {
        if (string.IsNullOrWhiteSpace(localSyncRoot))
            return [];

        string relativePath = remotePath.Trim('/');
        string localFolder = relativePath.Length == 0 ? localSyncRoot : localSyncRoot.CombinePath(relativePath);

        if (!fileSystem.Directory.Exists(localFolder))
            return [];

        return [.. fileSystem.Directory.EnumerateDirectories(localFolder)
            .Select(fileSystem.DirectoryInfo.New)
            .Where(directory => !directory.Attributes.HasFlag(FileAttributes.Hidden) && !directory.Name.StartsWith('.'))
            .Select(directory => directory.Name)];
    }
}

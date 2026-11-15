using System.IO.Abstractions;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Logging;

/// <summary>Points the log viewer at the folder <see cref="CloudSyncLogging"/> writes to (#1411).</summary>
/// <param name="fileSystem">Makes the folder handle.</param>
public sealed class CloudSyncLogFileLocator(IFileSystem fileSystem) : ILogFileLocator
{
    /// <inheritdoc/>
    public IDirectoryInfo GetLogFolder() => fileSystem.DirectoryInfo.New(CloudSyncLogging.FolderPath());
}

using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class LocalFolderFilter : ILocalFolderFilter
{
    /// <inheritdoc />
    public LocalFolderVisibility ShouldSkip(IDirectoryInfo folder) => LocalFolderVisibilityFactory.CreateVisible();
}

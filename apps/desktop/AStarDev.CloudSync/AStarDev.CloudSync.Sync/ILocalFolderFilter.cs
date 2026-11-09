using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

/// <summary>Decides which local folders the tree, the watcher and the scan leave out (#1395, #1399). All of them use this one filter so they agree.</summary>
public interface ILocalFolderFilter
{
    /// <summary>Judges a folder: hidden folders, dot-prefixed folders, symbolic links and junctions, and the app's own temporary download items are skipped.</summary>
    /// <param name="folder">The folder.</param>
    LocalFolderVisibility ShouldSkip(IDirectoryInfo folder);
}

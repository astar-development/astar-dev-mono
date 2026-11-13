using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

/// <summary>Decides which local files the scan leaves out (#1399). The folder walk uses <see cref="ILocalFolderFilter"/>.</summary>
public interface ILocalFileFilter
{
    /// <summary>Judges a file: symbolic links, hidden files, the app's own temp download files, Office lock files and operating system files are skipped. Dot-prefixed files sync.</summary>
    /// <param name="file">The file.</param>
    LocalFileVisibility ShouldSkip(IFileInfo file);
}

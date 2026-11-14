using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The local file changes a sync pass makes (#1400). The sync engine goes through it so tests can fail each step.</summary>
public interface ILocalFileOperations
{
    /// <summary>Creates the folder and any missing parents. An existing folder is not an error.</summary>
    /// <param name="folder">The folder.</param>
    /// <param name="cancellationToken">Cancels a retry wait.</param>
    /// <returns>Success, or why the folder could not be created.</returns>
    Task<Result<Unit, LocalOperationFailure>> CreateFolderAsync(IDirectoryInfo folder, CancellationToken cancellationToken);

    /// <summary>Moves a file or folder, creating the target's parent folder first. It never overwrites.</summary>
    /// <param name="source">The item to move.</param>
    /// <param name="targetPath">The full path it moves to.</param>
    /// <param name="cancellationToken">Cancels a retry wait.</param>
    /// <returns>Success, or why the move failed. A locked item is retried briefly first.</returns>
    Task<Result<Unit, LocalOperationFailure>> MoveAsync(IFileSystemInfo source, string targetPath, CancellationToken cancellationToken);

    /// <summary>Deletes a file or an empty folder. An item that is already gone counts as deleted.</summary>
    /// <param name="item">The item.</param>
    /// <param name="cancellationToken">Cancels a retry wait.</param>
    /// <returns>Success, or why the delete failed.</returns>
    Task<Result<Unit, LocalOperationFailure>> DeleteAsync(IFileSystemInfo item, CancellationToken cancellationToken);

    /// <summary>Moves a file or a whole folder into the <c>.sync-trash</c> folder of the sync root instead of deleting it (#1583). Every call gets its own trash folder, so nothing already trashed is overwritten. An item that is already gone or already in the trash counts as trashed. A failed move is never turned into a delete.</summary>
    /// <param name="root">The sync root that holds the item and the trash.</param>
    /// <param name="item">The item.</param>
    /// <param name="cancellationToken">Cancels a retry wait.</param>
    /// <returns>Success, or why the move failed. A locked item is retried briefly first.</returns>
    Task<Result<Unit, LocalOperationFailure>> TrashAsync(IDirectoryInfo root, IFileSystemInfo item, CancellationToken cancellationToken);

    /// <summary>Moves a finished download over the target and sets the target's last write time.</summary>
    /// <param name="download">The finished temp file.</param>
    /// <param name="target">The file to replace.</param>
    /// <param name="lastWriteUtc">The last write time to set, in UTC.</param>
    /// <param name="cancellationToken">Cancels a retry wait.</param>
    /// <returns>Success, or why the replace failed. A locked target is retried briefly first.</returns>
    Task<Result<Unit, LocalOperationFailure>> ReplaceAsync(IFileInfo download, IFileInfo target, DateTimeOffset lastWriteUtc, CancellationToken cancellationToken);
}

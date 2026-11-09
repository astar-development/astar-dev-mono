using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Lists the folders under an account's local sync root, one level at a time (#1395).</summary>
public interface ILocalFolderBrowser
{
    /// <summary>Lists the direct child folders of <paramref name="parent"/>, sorted by name ignoring case. Files, and folders the <see cref="ILocalFolderFilter"/> skips, are left out. A child that cannot be read is still listed, without sub-folders. The listing runs off the calling thread and stops when the token is cancelled.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="parent">The folder to list, relative to the account's sync root; <see cref="LocalRelativePathFactory.Root"/> lists the root.</param>
    /// <param name="cancellationToken">Cancels the listing.</param>
    /// <returns>The folders, or why the listing failed. It never throws for a missing or unreadable folder.</returns>
    Task<Result<IReadOnlyList<LocalFolder>, LocalFolderError>> GetChildFoldersAsync(AccountId accountId, LocalRelativePath parent, CancellationToken cancellationToken);
}

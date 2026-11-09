using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="syncRoots">Finds the account's sync root.</param>
/// <param name="filter">Decides which folders are left out.</param>
/// <param name="logger">Reports a child folder that cannot be read.</param>
public sealed class LocalFolderBrowser(ISyncRootReader syncRoots, ILocalFolderFilter filter, ILogger<LocalFolderBrowser> logger) : ILocalFolderBrowser
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<LocalFolder>, LocalFolderError>> GetChildFoldersAsync(AccountId accountId, LocalRelativePath parent, CancellationToken cancellationToken)
    {
        _ = (syncRoots, filter, logger);

        return Task.FromResult(Result.Failure<IReadOnlyList<LocalFolder>, LocalFolderError>(LocalFolderErrorFactory.CreateRootUnavailable()));
    }
}

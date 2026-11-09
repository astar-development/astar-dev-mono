using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScriptedRemoteBrowser : IRemoteFolderBrowser
{
    public Result<IReadOnlyList<RemoteFolder>, ProviderError> Listing { get; set; } = Result.Success<IReadOnlyList<RemoteFolder>, ProviderError>([]);

    public List<Option<RemoteItemId>> Parents { get; } = [];

    public Task<Result<IReadOnlyList<RemoteFolder>, ProviderError>> GetChildFoldersAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken)
    {
        Parents.Add(parent);

        return Task.FromResult(Listing);
    }
}

public sealed class ScriptedLocalBrowser : ILocalFolderBrowser
{
    public Result<IReadOnlyList<LocalFolder>, LocalFolderError> Listing { get; set; } = Result.Success<IReadOnlyList<LocalFolder>, LocalFolderError>([]);

    public List<LocalRelativePath> Parents { get; } = [];

    public Func<CancellationToken, Task>? Wait { get; set; }

    public async Task<Result<IReadOnlyList<LocalFolder>, LocalFolderError>> GetChildFoldersAsync(AccountId accountId, LocalRelativePath parent, CancellationToken cancellationToken)
    {
        Parents.Add(parent);
        if (Wait is not null)
            await Wait(cancellationToken);

        return Listing;
    }
}

public sealed class ScriptedRuleReader : ISelectionRuleReader
{
    public Result<IReadOnlyList<SelectionRule>, ErrorResponse> Listing { get; set; } = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([]);

    public Task<Result<IReadOnlyList<SelectionRule>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Listing);
}

public sealed class ScriptedCapability(Result<IRemoteFolderBrowser, ProviderError> outcome) : IProviderCapability<IRemoteFolderBrowser>
{
    public Result<IRemoteFolderBrowser, ProviderError> ForProvider(CloudProvider provider) => outcome;

    public Task<Result<IRemoteFolderBrowser, ProviderError>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(outcome);
}

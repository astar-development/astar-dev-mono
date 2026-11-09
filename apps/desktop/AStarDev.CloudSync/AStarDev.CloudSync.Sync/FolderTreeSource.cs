using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="remoteBrowsers">Finds the account's cloud folder browser.</param>
/// <param name="localBrowser">Lists the folders on disk.</param>
/// <param name="rules">Reads the stored selection rules.</param>
/// <param name="evaluator">Judges each folder against the rules.</param>
public sealed class FolderTreeSource(IProviderCapability<IRemoteFolderBrowser> remoteBrowsers, ILocalFolderBrowser localBrowser, ISelectionRuleReader rules, ISyncSelectionEvaluator evaluator) : IFolderTreeSource
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>> GetChildrenAsync(AccountId accountId, FolderTreePosition parent, CancellationToken cancellationToken)
    {
        _ = (remoteBrowsers, localBrowser, rules, evaluator);

        return Task.FromResult(Result.Failure<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(FolderTreeFailureFactory.CreateRulesFailed(new ErrorResponse("not implemented"))));
    }
}

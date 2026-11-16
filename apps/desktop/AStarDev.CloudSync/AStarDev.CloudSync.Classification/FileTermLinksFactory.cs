using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="FileTermLinks"/> values.</summary>
public static class FileTermLinksFactory
{
    /// <summary>Creates the links, copying the term list.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The path below the sync root.</param>
    /// <param name="termIds">The terms that apply.</param>
    public static FileTermLinks Create(AccountId accountId, RelativeSyncPath path, IReadOnlyList<TermId> termIds) => new(accountId, path, [.. termIds]);
}

using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SelectionRule"/> values.</summary>
public static class SelectionRuleFactory
{
    /// <summary>Creates a rule.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The folder's path relative to the sync root.</param>
    /// <param name="remoteId">The remote folder's id, when known.</param>
    /// <param name="kind">What the rule says.</param>
    public static SelectionRule Create(AccountId accountId, LocalRelativePath path, Option<RemoteItemId> remoteId, SelectionRuleKind kind) => new(accountId, path, remoteId, kind);
}

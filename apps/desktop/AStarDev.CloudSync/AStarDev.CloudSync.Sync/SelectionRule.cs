using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>A stored choice about one folder of an account (#1397). The rules are the single source of truth for what syncs.</summary>
public sealed record SelectionRule
{
    internal SelectionRule(AccountId accountId, LocalRelativePath path, Option<RemoteItemId> remoteId, SelectionRuleKind kind)
    {
        AccountId = accountId;
        Path = path;
        RemoteId = remoteId;
        Kind = kind;
    }

    /// <summary>The account.</summary>
    public AccountId AccountId { get; }

    /// <summary>The folder's path relative to the account's sync root. The remote folder mirrors it.</summary>
    public LocalRelativePath Path { get; }

    /// <summary>The remote folder's id when it is known, so a rename or move on the cloud can keep the rule attached.</summary>
    public Option<RemoteItemId> RemoteId { get; }

    /// <summary>What the rule says.</summary>
    public SelectionRuleKind Kind { get; }
}
